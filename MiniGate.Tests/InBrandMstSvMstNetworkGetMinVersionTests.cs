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

public class InBrandMstSvMstNetworkGetMinVersionTests
{
    private static async Task<(AppDbContext db, InBrandControllerPort controller, SqliteConnection conn)> CreateTestContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        await db.Database.EnsureCreatedAsync();
        await PortSchema.EnsureAsync(db);

        var controller = new InBrandControllerPort
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return (db, controller, conn);
    }

    [Fact]
    public async Task GetMinVersion_NullReq_ReturnsSuccessWithNullRemark()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_MstSv_Mst_Network_GetMinVersion(null, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstSvMstNetworkGetMinVersionResp>(okRes.Value);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);
            Assert.Null(resp.Data.Lst_MstSv_Mst_Network);
            var sysInfo = Assert.Single(resp.Data.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
            Assert.Null(sysInfo.Remark);
            Assert.Equal("0", sysInfo.ErrorCode);
        }
    }

    [Fact]
    public async Task GetMinVersion_EmptyReq_ReturnsSuccessWithNullRemark()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_MstSv_Mst_Network_GetMinVersion(new InBrandMstSvMstNetworkGetMinVersionReq(), db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstSvMstNetworkGetMinVersionResp>(okRes.Value);
            Assert.True(resp.Success);
            var sysInfo = Assert.Single(resp.Data!.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
            Assert.Null(sysInfo.Remark);
        }
    }

    [Fact]
    public async Task GetMinVersion_NetworkId01_ReturnsSuccessWithMinVersion100()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_MstSv_Mst_Network_GetMinVersion(new InBrandMstSvMstNetworkGetMinVersionReq
            {
                NetworkID = "NET_TEST_01",
                Tid = "TID_NET_01"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstSvMstNetworkGetMinVersionResp>(okRes.Value);
            Assert.True(resp.Success);
            var sysInfo = Assert.Single(resp.Data!.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
            Assert.Equal("1.0.0", sysInfo.Remark);
            Assert.Equal("TID_NET_01", sysInfo.Tid);
            Assert.Equal("0", sysInfo.ErrorCode);
        }
    }

    [Fact]
    public async Task GetMinVersion_NetworkId02_ReturnsSuccessWithMinVersion234()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_MstSv_Mst_Network_GetMinVersion(new InBrandMstSvMstNetworkGetMinVersionReq
            {
                NetworkID = "NET_TEST_02",
                Tid = "TID_NET_02"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstSvMstNetworkGetMinVersionResp>(okRes.Value);
            Assert.True(resp.Success);
            var sysInfo = Assert.Single(resp.Data!.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
            Assert.Equal("2.3.4", sysInfo.Remark);
            Assert.Equal("TID_NET_02", sysInfo.Tid);
            Assert.Equal("0", sysInfo.ErrorCode);
        }
    }

    [Fact]
    public async Task GetMinVersion_NetworkIdCaseInsensitive_ReturnsSuccessWithMinVersion100()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            // net_test_01 chữ thường phải khớp NET_TEST_01
            var res = await controller.WA_MstSv_Mst_Network_GetMinVersion(new InBrandMstSvMstNetworkGetMinVersionReq
            {
                NetworkID = "net_test_01"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstSvMstNetworkGetMinVersionResp>(okRes.Value);
            Assert.True(resp.Success);
            var sysInfo = Assert.Single(resp.Data!.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
            Assert.Equal("1.0.0", sysInfo.Remark);
        }
    }

    [Fact]
    public async Task GetMinVersion_NetworkIdNotExist_ReturnsSuccessWithNullRemark()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_MstSv_Mst_Network_GetMinVersion(new InBrandMstSvMstNetworkGetMinVersionReq
            {
                NetworkID = "NET_NON_EXISTENT"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstSvMstNetworkGetMinVersionResp>(okRes.Value);
            Assert.True(resp.Success);
            var sysInfo = Assert.Single(resp.Data!.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
            Assert.Null(sysInfo.Remark);
        }
    }

    [Fact]
    public async Task GetMinVersion_HeaderNetworkId_ReturnsSuccessWithMinVersion()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            controller.ControllerContext.HttpContext.Request.Headers["networkId"] = "NET_TEST_02";
            var res = await controller.WA_MstSv_Mst_Network_GetMinVersion(null, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstSvMstNetworkGetMinVersionResp>(okRes.Value);
            Assert.True(resp.Success);
            var sysInfo = Assert.Single(resp.Data!.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
            Assert.Equal("2.3.4", sysInfo.Remark);
        }
    }

    [Fact]
    public async Task GetMinVersion_RealServer_OnPort8556_RoundTripAndFilters()
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "test_port_8556.db");
        if (File.Exists(dbPath)) File.Delete(dbPath);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:8556");
        builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
        builder.Services.AddScoped<ITenantContext, TenantContext>();
        builder.Services.AddControllers(o =>
        {
            o.Filters.Add<PortJsonResultFilter>();
            o.Filters.Add<PortExceptionFilter>();
        }).AddApplicationPart(typeof(InBrandControllerPort).Assembly);

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
            using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8556") };

            // Ca 1: Body rong {}
            var res1 = await client.PostAsync("/api/inbrand/wa_mstsv_mst_network_getminversion", new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
            var json1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(json1).RootElement;
            Assert.True(doc1.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc1.GetProperty("Data").GetProperty("c_K_DT_Sys").GetProperty("Lst_c_K_DT_SysInfo")[0].GetProperty("Remark").ValueKind);

            // Ca 2: Loc NetworkID = "NET_TEST_01" -> MinVersion = "1.0.0"
            var res2 = await client.PostAsync("/api/inbrand/wa_mstsv_mst_network_getminversion", new StringContent("{\"NetworkID\":\"NET_TEST_01\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
            var json2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(json2).RootElement;
            Assert.True(doc2.GetProperty("Success").GetBoolean());
            Assert.Equal("1.0.0", doc2.GetProperty("Data").GetProperty("c_K_DT_Sys").GetProperty("Lst_c_K_DT_SysInfo")[0].GetProperty("Remark").GetString());

            // Ca 3: Loc NetworkID = "NET_TEST_02" -> MinVersion = "2.3.4" (2 bo loc ra 2 gia tri khac nhau)
            var res3 = await client.PostAsync("/api/inbrand/wa_mstsv_mst_network_getminversion", new StringContent("{\"NetworkID\":\"NET_TEST_02\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res3.StatusCode);
            var json3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(json3).RootElement;
            Assert.True(doc3.GetProperty("Success").GetBoolean());
            Assert.Equal("2.3.4", doc3.GetProperty("Data").GetProperty("c_K_DT_Sys").GetProperty("Lst_c_K_DT_SysInfo")[0].GetProperty("Remark").GetString());

            // Ca 4: Loc NetworkID qua Header "networkId: NET_TEST_01"
            var req4 = new HttpRequestMessage(HttpMethod.Post, "/api/inbrand/wa_mstsv_mst_network_getminversion")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            req4.Headers.Add("networkId", "NET_TEST_01");
            var res4 = await client.SendAsync(req4);
            Assert.Equal(HttpStatusCode.OK, res4.StatusCode);
            var json4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(json4).RootElement;
            Assert.True(doc4.GetProperty("Success").GetBoolean());
            Assert.Equal("1.0.0", doc4.GetProperty("Data").GetProperty("c_K_DT_Sys").GetProperty("Lst_c_K_DT_SysInfo")[0].GetProperty("Remark").GetString());

            // Ca 5: NetworkID khong ton tai -> Remark la null
            var res5 = await client.PostAsync("/api/inbrand/wa_mstsv_mst_network_getminversion", new StringContent("{\"NetworkID\":\"NET_NOT_EXIST\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res5.StatusCode);
            var json5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(json5).RootElement;
            Assert.True(doc5.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc5.GetProperty("Data").GetProperty("c_K_DT_Sys").GetProperty("Lst_c_K_DT_SysInfo")[0].GetProperty("Remark").ValueKind);

            // Ca 6: Round-trip: Them moi mot ban ghi Network moi vao DB va doc lai qua endpoint
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Set<MstSv_Mst_Network>().Add(new MstSv_Mst_Network
                {
                    NetworkID = "NET_ROUND_TRIP",
                    NetworkName = "Mạng round trip",
                    MinVersion = "5.0.0",
                    FlagActive = "1"
                });
                await db.SaveChangesAsync();
            }

            var res6 = await client.PostAsync("/api/inbrand/wa_mstsv_mst_network_getminversion", new StringContent("{\"NetworkID\":\"NET_ROUND_TRIP\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res6.StatusCode);
            var json6 = await res6.Content.ReadAsStringAsync();
            var doc6 = JsonDocument.Parse(json6).RootElement;
            Assert.True(doc6.GetProperty("Success").GetBoolean());
            Assert.Equal("5.0.0", doc6.GetProperty("Data").GetProperty("c_K_DT_Sys").GetProperty("Lst_c_K_DT_SysInfo")[0].GetProperty("Remark").GetString());
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
