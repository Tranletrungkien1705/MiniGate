using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniGate.Data;
using MiniGate.Models;
using MiniGate.Services;
using Xunit;

namespace MiniGate.Tests;

/// <summary>Test admin gateway: lưu/cập nhật tuyến, bật-tắt, cấp API key client, dashboard tổng hợp log.</summary>
public class GateAdminServiceTests
{
    private static (AppDbContext db, IGateAdminService svc, SqliteConnection conn) NewSvc()
    {
        var conn = new SqliteConnection("DataSource=:memory:"); conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        return (db, new GateAdminService(db), conn);
    }

    [Fact]
    public async Task SaveRoute_Insert_ThenUpdate()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var id = await svc.SaveRouteAsync(new GwRoute { Prefix = "pim", UpstreamBaseUrl = "https://minipim.onrender.com", Name = "PIM" });
            Assert.True(id > 0);
            await svc.SaveRouteAsync(new GwRoute { Id = id, Prefix = "pim", UpstreamBaseUrl = "https://x.onrender.com", Name = "PIM2" });
            var routes = await svc.RoutesAsync();
            Assert.Single(routes);
            Assert.Equal("PIM2", routes[0].Name);
        }
    }

    [Fact]
    public async Task ToggleRoute_FlipsActive()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var id = await svc.SaveRouteAsync(new GwRoute { Prefix = "wms", UpstreamBaseUrl = "https://miniwms.onrender.com" });
            var before = (await svc.RoutesAsync())[0].IsActive;
            await svc.ToggleRouteAsync(id);
            Assert.NotEqual(before, (await svc.RoutesAsync())[0].IsActive);
        }
    }

    [Fact]
    public async Task CreateClient_GeneratesApiKey()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            await svc.CreateClientAsync("App A", 100);
            var c = (await svc.ClientsAsync()).First();
            Assert.False(string.IsNullOrEmpty(c.ApiKey));
            Assert.Equal(100, c.RateLimitPerMin);
        }
    }

    [Fact]
    public async Task Dashboard_AggregatesLogs()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            await svc.SaveRouteAsync(new GwRoute { Prefix = "pim", UpstreamBaseUrl = "https://x" });
            db.Logs.Add(new RequestLog { OrgId = TenantContext.DefaultOrgId, RouteName = "PIM", Method = "GET", Path = "/gw/pim", StatusCode = 200, LatencyMs = 100, At = DateTime.Now });
            db.Logs.Add(new RequestLog { OrgId = TenantContext.DefaultOrgId, RouteName = "PIM", Method = "GET", Path = "/gw/pim", StatusCode = 502, LatencyMs = 300, At = DateTime.Now });
            await db.SaveChangesAsync();
            var d = await svc.DashboardAsync();
            Assert.Equal(2, d.Requests24h);
            Assert.Equal(200, d.AvgLatency);   // (100+300)/2
            Assert.Contains(d.ByStatus, x => x.Code == 502);
        }
    }
}
