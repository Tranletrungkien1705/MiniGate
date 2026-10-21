using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniGate.Controllers.Port;
using MiniGate.Data;
using MiniGate.Models.Port;
using Xunit;

namespace MiniGate.Tests;

// Hạng mục #166 — EQRController/WA_ELTS_eTEM_Search (port 1:1).
// Nguồn: EQRController.cs:16097-16246 -> ElasticSearch.cs:2168-2306 (WAS_) -> ElasticSearch.cs:1436-1723 (ELTS_eTEM_Search).
public class EQREltsEtemSearchTests
{
    private static async Task<(AppDbContext db, EQRControllerPort controller, SqliteConnection conn)> CreateTestContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        await db.Database.EnsureCreatedAsync();
        await PortSchema.EnsureAsync(db);

        var controller = new EQRControllerPort
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return (db, controller, conn);
    }

    [Fact]
    public async Task Search_NonUrl_ReturnsErrIU()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_ELTS_eTEM_Search(new EQREltsEtemSearchReq
            {
                Tid = "TID_IU",
                URI = "abc"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<EQREltsEtemSearchResp>(okRes.Value);
            Assert.True(resp.Success);
            Assert.Equal("ERR_IU", resp.Data!.ELTSBizCodeReturn);
            Assert.Equal("abc", resp.Data.TrackingCode);
            Assert.Equal("abc", resp.Data.RefInfo);
            Assert.Equal("TID_IU", Assert.Single(resp.Data.c_K_DT_Sys.Lst_c_K_DT_SysInfo).Tid);
        }
    }

    [Fact]
    public async Task Search_UrlDomainNotConfigured_ReturnsErrAUS()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_ELTS_eTEM_Search(new EQREltsEtemSearchReq
            {
                URI = "https://unknown.test.vn/search?pf=1"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<EQREltsEtemSearchResp>(okRes.Value);
            Assert.True(resp.Success);
            Assert.Equal("ERR_AUS", resp.Data!.ELTSBizCodeReturn);
            Assert.Equal("https://unknown.test.vn/search?pf=1", resp.Data.TrackingCode);
        }
    }

    [Fact]
    public async Task Search_UrlDomainInactive_ReturnsErrAUS()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            // etem.inactive.vn có trong Mst_DomainConfig nhưng FlagActive = '0' -> nguồn lọc FlagActive = '1' nên không thấy
            var res = await controller.WA_ELTS_eTEM_Search(new EQREltsEtemSearchReq
            {
                URI = "http://etem.inactive.vn/search"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<EQREltsEtemSearchResp>(okRes.Value);
            Assert.Equal("ERR_AUS", resp.Data!.ELTSBizCodeReturn);
        }
    }

    [Fact]
    public async Task Search_UrlDomainConfigured_ExternalServiceGap_ReturnsNullBizCode()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            // etem.test.vn có FlagActive = '1' -> nguồn gọi dịch vụ ngoài ElasticSearch (GAP ở đích)
            var res = await controller.WA_ELTS_eTEM_Search(new EQREltsEtemSearchReq
            {
                URI = "https://etem.test.vn/search?pf=1"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<EQREltsEtemSearchResp>(okRes.Value);
            Assert.True(resp.Success);
            Assert.Null(resp.Data!.ELTSBizCodeReturn);
            Assert.Null(resp.Data.TrackingCode);
            Assert.Null(resp.Data.RefInfo);
        }
    }

    [Fact]
    public async Task Search_RealServer_OnFreePort_FiltersAndGuard()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), $"test_port_{port}_{Guid.NewGuid():N}.db");
        if (File.Exists(dbPath)) File.Delete(dbPath);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
        builder.Services.AddScoped<ITenantContext, TenantContext>();
        builder.Services.AddControllers(o =>
        {
            o.Filters.Add<PortJsonResultFilter>();
            o.Filters.Add<PortExceptionFilter>();
        }).AddApplicationPart(typeof(EQRControllerPort).Assembly);

        var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            await PortSchema.EnsureAsync(db);
        }
        app.MapControllers();
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

            // Ca 1: URI không phải URL -> ERR_IU
            var res1 = await client.PostAsync("/api/eqr/wa_elts_etem_search",
                new StringContent("{\"URI\":\"abc\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
            var doc1 = JsonDocument.Parse(await res1.Content.ReadAsStringAsync()).RootElement;
            Assert.True(doc1.GetProperty("Success").GetBoolean());
            Assert.Equal("ERR_IU", doc1.GetProperty("Data").GetProperty("ELTSBizCodeReturn").GetString());

            // Ca 2: tên miền không cấu hình -> ERR_AUS
            var res2 = await client.PostAsync("/api/eqr/wa_elts_etem_search",
                new StringContent("{\"URI\":\"https://unknown.test.vn/x\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
            var doc2 = JsonDocument.Parse(await res2.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal("ERR_AUS", doc2.GetProperty("Data").GetProperty("ELTSBizCodeReturn").GetString());

            // Ca 3: tên miền cấu hình FlagActive = '1' -> qua guard, dịch vụ ngoài GAP -> ELTSBizCodeReturn null
            var res3 = await client.PostAsync("/api/eqr/wa_elts_etem_search",
                new StringContent("{\"URI\":\"https://etem.test.vn/x\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res3.StatusCode);
            var doc3 = JsonDocument.Parse(await res3.Content.ReadAsStringAsync()).RootElement;
            Assert.True(doc3.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc3.GetProperty("Data").GetProperty("ELTSBizCodeReturn").ValueKind);

            // Ca 4: body rỗng -> nguồn ném NullReference (strUri null) -> PortExceptionFilter trả 400
            var res4 = await client.PostAsync("/api/eqr/wa_elts_etem_search",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, res4.StatusCode);
            var doc4 = JsonDocument.Parse(await res4.Content.ReadAsStringAsync()).RootElement;
            Assert.False(doc4.GetProperty("Success").GetBoolean());
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }
}
