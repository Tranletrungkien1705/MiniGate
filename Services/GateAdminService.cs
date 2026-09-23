using Microsoft.EntityFrameworkCore;
using MiniGate.Data;
using MiniGate.Models;

namespace MiniGate.Services;

public record GateDash(int Routes, int ActiveRoutes, int Clients, int Requests24h, double AvgLatency,
    List<(int Code, int Count)> ByStatus, List<(string Route, int Count)> TopRoutes);

public interface IGateAdminService
{
    Task<List<GwRoute>> RoutesAsync();
    Task<int> SaveRouteAsync(GwRoute r);
    Task ToggleRouteAsync(int id);
    Task<List<ApiClient>> ClientsAsync();
    Task<int> CreateClientAsync(string name, int rateLimit);
    Task ToggleClientAsync(int id);
    Task<List<RequestLog>> LogsAsync(int take = 100);
    Task<GateDash> DashboardAsync();
}

public class GateAdminService(AppDbContext db) : IGateAdminService
{
    public Task<List<GwRoute>> RoutesAsync() => db.Routes.OrderBy(r => r.Prefix).ToListAsync();

    public async Task<int> SaveRouteAsync(GwRoute r)
    {
        if (r.Id > 0)
        {
            var t = await db.Routes.FirstAsync(x => x.Id == r.Id);
            t.Name = r.Name; t.Prefix = r.Prefix.Trim().Trim('/'); t.UpstreamBaseUrl = r.UpstreamBaseUrl.Trim();
            t.RequireAuth = r.RequireAuth; t.TimeoutSeconds = r.TimeoutSeconds;
        }
        else { r.Prefix = r.Prefix.Trim().Trim('/'); db.Routes.Add(r); }
        await db.SaveChangesAsync();
        return r.Id;
    }

    public async Task ToggleRouteAsync(int id)
    {
        var r = await db.Routes.FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        r.IsActive = !r.IsActive; await db.SaveChangesAsync();
    }

    public Task<List<ApiClient>> ClientsAsync() => db.Clients.OrderBy(c => c.Name).ToListAsync();

    public async Task<int> CreateClientAsync(string name, int rateLimit)
    {
        var c = new ApiClient { Name = name.Trim(), ApiKey = "gk_" + Guid.NewGuid().ToString("N"), RateLimitPerMin = rateLimit <= 0 ? 60 : rateLimit };
        db.Clients.Add(c); await db.SaveChangesAsync();
        return c.Id;
    }

    public async Task ToggleClientAsync(int id)
    {
        var c = await db.Clients.FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        c.IsActive = !c.IsActive; await db.SaveChangesAsync();
    }

    public async Task<List<RequestLog>> LogsAsync(int take = 100) =>
        await db.Logs.OrderByDescending(l => l.At).Take(take).ToListAsync();

    public async Task<GateDash> DashboardAsync()
    {
        var since = DateTime.Now.AddHours(-24);
        var logs = await db.Logs.Where(l => l.At >= since).ToListAsync();
        var byStatus = logs.GroupBy(l => l.StatusCode).Select(g => (g.Key, g.Count())).OrderBy(x => x.Key).ToList();
        var topRoutes = logs.GroupBy(l => l.RouteName).Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).Take(6).ToList();
        return new GateDash(
            await db.Routes.CountAsync(), await db.Routes.CountAsync(r => r.IsActive),
            await db.Clients.CountAsync(), logs.Count,
            logs.Count > 0 ? Math.Round(logs.Average(l => l.LatencyMs), 0) : 0,
            byStatus, topRoutes);
    }
}
