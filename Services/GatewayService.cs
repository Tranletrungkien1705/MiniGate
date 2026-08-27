using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MiniGate.Data;
using MiniGate.Models;

namespace MiniGate.Services;

/// <summary>Giới hạn tần suất theo client, cửa sổ 1 phút (in-memory).</summary>
public sealed class RateLimiter
{
    private readonly ConcurrentDictionary<string, int> _buckets = new();

    /// <returns>true nếu còn hạn mức; false nếu vượt.</returns>
    public bool Allow(int clientId, int limitPerMin)
    {
        if (limitPerMin <= 0) return true;
        var key = $"{clientId}:{DateTime.UtcNow:yyyyMMddHHmm}";
        var n = _buckets.AddOrUpdate(key, 1, (_, v) => v + 1);
        if (_buckets.Count > 2000) foreach (var k in _buckets.Keys) if (!k.EndsWith(DateTime.UtcNow.ToString("HHmm"))) _buckets.TryRemove(k, out _);
        return n <= limitPerMin;
    }
}

/// <summary>Định tuyến &amp; chuyển tiếp request /gw/{prefix}/... tới upstream, kèm auth + rate limit + log.</summary>
public sealed class GatewayProxy(IServiceProvider sp, IHttpClientFactory httpFactory, RateLimiter limiter)
{
    private static readonly HashSet<string> HopByHop = new(StringComparer.OrdinalIgnoreCase)
        { "Host", "Connection", "Keep-Alive", "Transfer-Encoding", "Upgrade", "Proxy-Connection", "X-Api-Key" };

    public async Task InvokeAsync(HttpContext ctx, string path)
    {
        var sw = Stopwatch.StartNew();
        var parts = (path ?? "").Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        var prefix = parts.Length > 0 ? parts[0] : "";
        var rest = parts.Length > 1 ? parts[1] : "";

        using var scope = sp.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Client (nếu có X-Api-Key) — xác định org của tuyến.
        var apiKey = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
        ApiClient? client = string.IsNullOrWhiteSpace(apiKey) ? null
            : await db.Clients.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ApiKey == apiKey && c.IsActive);
        tenant.OrgId = client?.OrgId ?? TenantContext.DefaultOrgId;

        var route = await db.Routes.FirstOrDefaultAsync(r => r.Prefix == prefix && r.IsActive);
        async Task Log(int status, string upstream, string routeName)
        {
            db.Logs.Add(new RequestLog { OrgId = tenant.OrgId, RouteId = route?.Id, RouteName = routeName,
                ClientName = client?.Name ?? "anonymous", Method = ctx.Request.Method, Path = "/" + (path ?? ""),
                UpstreamUrl = upstream, StatusCode = status, LatencyMs = sw.ElapsedMilliseconds });
            try { await db.SaveChangesAsync(); } catch { }
        }

        if (route == null) { ctx.Response.StatusCode = 404; await ctx.Response.WriteAsJsonAsync(new { error = $"Không có tuyến cho '/{prefix}'." }); await Log(404, "", prefix); return; }
        if (route.RequireAuth && client == null) { ctx.Response.StatusCode = 401; await ctx.Response.WriteAsJsonAsync(new { error = "Tuyến yêu cầu X-Api-Key hợp lệ." }); await Log(401, "", route.Name); return; }
        if (client != null && !limiter.Allow(client.Id, client.RateLimitPerMin)) { ctx.Response.StatusCode = 429; await ctx.Response.WriteAsJsonAsync(new { error = $"Vượt hạn mức {client.RateLimitPerMin} req/phút." }); await Log(429, "", route.Name); return; }

        var upstreamUrl = route.UpstreamBaseUrl.TrimEnd('/') + "/" + rest + ctx.Request.QueryString;
        try
        {
            var http = httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(Math.Clamp(route.TimeoutSeconds, 1, 120));
            var req = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), upstreamUrl);
            if (ctx.Request.ContentLength > 0 || ctx.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                ctx.Request.EnableBuffering();
                var ms = new MemoryStream();
                await ctx.Request.Body.CopyToAsync(ms);
                ms.Position = 0;
                req.Content = new StreamContent(ms);
                if (ctx.Request.ContentType is { } ct) req.Content.Headers.TryAddWithoutValidation("Content-Type", ct);
            }
            foreach (var h in ctx.Request.Headers)
                if (!HopByHop.Contains(h.Key) && !h.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                    req.Headers.TryAddWithoutValidation(h.Key, h.Value.ToArray());

            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.RequestAborted);
            ctx.Response.StatusCode = (int)resp.StatusCode;
            foreach (var h in resp.Content.Headers)
                if (!h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    ctx.Response.Headers[h.Key] = h.Value.ToArray();
            ctx.Response.Headers["X-Gateway-Route"] = route.Name;
            await resp.Content.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
            await Log((int)resp.StatusCode, upstreamUrl, route.Name);
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = 502;
            await ctx.Response.WriteAsJsonAsync(new { error = "Upstream lỗi/không phản hồi.", detail = ex.Message });
            await Log(502, upstreamUrl, route.Name);
        }
    }
}
