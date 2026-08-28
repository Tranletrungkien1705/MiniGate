using Microsoft.AspNetCore.Mvc;
using MiniGate.Data;
using MiniGate.Models;
using MiniGate.Services;

namespace MiniGate.Controllers;

/// <summary>
/// API JSON cho SPA React (admin gateway). DTO phẳng. Dashboard cache Redis 20s theo tenant (X-Cache).
/// Quản lý tuyến (route → upstream), client (API key + rate limit), nhật ký request. Proxy /gw/{**path} giữ nguyên.
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class ApiV1Controller(IGateAdminService svc, ICache cache, ITenantContext tenant) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var key = $"gate:dash:{tenant.OrgId}";
        var hit = await cache.GetAsync<DashDto>(key);
        if (hit != null) { Response.Headers["X-Cache"] = "HIT"; return Ok(hit); }
        var d = await svc.DashboardAsync();
        var dto = new DashDto(d.Routes, d.ActiveRoutes, d.Clients, d.Requests24h, d.AvgLatency,
            d.ByStatus.Select(x => new CodeCountDto(x.Code, x.Count)).ToList(),
            d.TopRoutes.Select(x => new RouteCountDto(x.Route, x.Count)).ToList());
        await cache.SetAsync(key, dto, TimeSpan.FromSeconds(20));
        Response.Headers["X-Cache"] = "MISS";
        return Ok(dto);
    }

    [HttpGet("routes")]
    public async Task<IActionResult> Routes()
        => Ok((await svc.RoutesAsync()).Select(r => new { r.Id, r.Name, r.Prefix, r.UpstreamBaseUrl, r.RequireAuth, r.TimeoutSeconds, r.IsActive }));

    [HttpPost("routes")]
    public async Task<IActionResult> SaveRoute([FromBody] RouteReq r)
    {
        if (string.IsNullOrWhiteSpace(r.Prefix) || string.IsNullOrWhiteSpace(r.UpstreamBaseUrl))
            return BadRequest(new { error = "Cần prefix và upstream URL." });
        var id = await svc.SaveRouteAsync(new GwRoute
        {
            Id = r.Id, Name = r.Name ?? r.Prefix, Prefix = r.Prefix.Trim(), UpstreamBaseUrl = r.UpstreamBaseUrl.Trim(),
            RequireAuth = r.RequireAuth, TimeoutSeconds = r.TimeoutSeconds <= 0 ? 30 : r.TimeoutSeconds, IsActive = true
        });
        return Ok(new { id });
    }

    [HttpPost("routes/{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        await svc.ToggleRouteAsync(id);
        return Ok(new { ok = true });
    }

    [HttpGet("clients")]
    public async Task<IActionResult> Clients()
        => Ok((await svc.ClientsAsync()).Select(c => new { c.Id, c.Name, c.ApiKey, c.RateLimitPerMin, c.IsActive, c.CreatedAt }));

    [HttpPost("clients")]
    public async Task<IActionResult> CreateClient([FromBody] ClientReq r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) return BadRequest(new { error = "Cần tên client." });
        var id = await svc.CreateClientAsync(r.Name.Trim(), r.RateLimitPerMin <= 0 ? 60 : r.RateLimitPerMin);
        return Ok(new { id });
    }

    [HttpGet("logs")]
    public async Task<IActionResult> Logs([FromQuery] int take = 100)
        => Ok((await svc.LogsAsync(take)).Select(l => new { l.Id, l.RouteName, l.ClientName, l.Method, l.Path, l.UpstreamUrl, l.StatusCode, l.LatencyMs, l.At }));
}

public record DashDto(int Routes, int ActiveRoutes, int Clients, int Requests24h, double AvgLatency, List<CodeCountDto> ByStatus, List<RouteCountDto> TopRoutes);
public record CodeCountDto(int Code, int Count);
public record RouteCountDto(string Route, int Count);

public class RouteReq { public int Id { get; set; } public string? Name { get; set; } public string Prefix { get; set; } = ""; public string UpstreamBaseUrl { get; set; } = ""; public bool RequireAuth { get; set; } public int TimeoutSeconds { get; set; } }
public class ClientReq { public string Name { get; set; } = ""; public int RateLimitPerMin { get; set; } }
