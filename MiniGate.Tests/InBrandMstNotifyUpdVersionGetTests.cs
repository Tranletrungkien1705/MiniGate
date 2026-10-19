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

public class InBrandMstNotifyUpdVersionGetTests
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
    public async Task GetNotifyUpdVersion_NullReq_ReturnsSuccessWithEmptyList()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_Mst_NotifyUpdVersion_Get(null, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstNotifyUpdVersionGetResp>(okRes.Value);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);
            Assert.Empty(resp.Data.Lst_Mst_NotifyUpdVersion!);
            var sysInfo = Assert.Single(resp.Data.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
            Assert.Equal("0", sysInfo.ErrorCode);
        }
    }

    [Fact]
    public async Task GetNotifyUpdVersion_EmptyReq_ReturnsSuccessWithEmptyList()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_Mst_NotifyUpdVersion_Get(new InBrandMstNotifyUpdVersionGetReq(), db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstNotifyUpdVersionGetResp>(okRes.Value);
            Assert.True(resp.Success);
            Assert.Empty(resp.Data!.Lst_Mst_NotifyUpdVersion!);
        }
    }

    [Fact]
    public async Task GetNotifyUpdVersion_NetworkNotFound_ReturnsEmptyList()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_Mst_NotifyUpdVersion_Get(new InBrandMstNotifyUpdVersionGetReq
            {
                NetworkID = "NET_NON_EXISTENT",
                AppVersion = "1.0.0"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstNotifyUpdVersionGetResp>(okRes.Value);
            Assert.True(resp.Success);
            Assert.Empty(resp.Data!.Lst_Mst_NotifyUpdVersion!);
        }
    }

    [Fact]
    public async Task GetNotifyUpdVersion_ForcedUpdate_ReturnsForcedUpdFlagsAndContent()
    {
        // NET_TEST_01: MinVersion="1.0.0", DefaultVersion="2.0.0"
        // AppVersion="0.9.0" < MinVersion < DefaultVersion -> FORCEDUPD
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_Mst_NotifyUpdVersion_Get(new InBrandMstNotifyUpdVersionGetReq
            {
                NetworkID = "NET_TEST_01",
                AppVersion = "0.9.0",
                Tid = "TID_FORCED"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstNotifyUpdVersionGetResp>(okRes.Value);
            Assert.True(resp.Success);
            var item = Assert.Single(resp.Data!.Lst_Mst_NotifyUpdVersion!);
            Assert.Equal("1", item.FlagNewVersion);
            Assert.Equal("1", item.FlagForcedUpdate);
            Assert.Equal("1", item.FlagStrongForcedUpdate);
            Assert.Equal("Phiên bản ứng dụng đã quá cũ, vui lòng cập nhật lên bản mới nhất.", item.NotifyContent);
            Assert.Equal("https://app.idocnet.com/download/forced", item.LinkUpdUrl);

            var sysInfo = Assert.Single(resp.Data.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
            Assert.Equal("TID_FORCED", sysInfo.Tid);
            Assert.Equal("0", sysInfo.ErrorCode);
        }
    }

    [Fact]
    public async Task GetNotifyUpdVersion_OptionalUpdate_ReturnsOptionalUpdFlagsAndContent()
    {
        // NET_TEST_01: MinVersion="1.0.0", DefaultVersion="2.0.0"
        // AppVersion="1.5.0" >= MinVersion and < DefaultVersion -> OPTIONALUPD
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_Mst_NotifyUpdVersion_Get(new InBrandMstNotifyUpdVersionGetReq
            {
                NetworkID = "NET_TEST_01",
                AppVersion = "1.5.0",
                Tid = "TID_OPTIONAL"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstNotifyUpdVersionGetResp>(okRes.Value);
            Assert.True(resp.Success);
            var item = Assert.Single(resp.Data!.Lst_Mst_NotifyUpdVersion!);
            Assert.Equal("1", item.FlagNewVersion);
            Assert.Equal("0", item.FlagForcedUpdate);
            Assert.Equal("0", item.FlagStrongForcedUpdate);
            Assert.Equal("Đã có phiên bản mới, vui lòng cập nhật để trải nghiệm tính năng mới.", item.NotifyContent);
            Assert.Equal("https://app.idocnet.com/download/optional", item.LinkUpdUrl);
        }
    }

    [Fact]
    public async Task GetNotifyUpdVersion_LatestVersion_ReturnsNoUpdate()
    {
        // NET_TEST_01: MinVersion="1.0.0", DefaultVersion="2.0.0"
        // AppVersion="2.0.0" >= DefaultVersion -> no update needed
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_Mst_NotifyUpdVersion_Get(new InBrandMstNotifyUpdVersionGetReq
            {
                NetworkID = "NET_TEST_01",
                AppVersion = "2.0.0"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstNotifyUpdVersionGetResp>(okRes.Value);
            Assert.True(resp.Success);
            var item = Assert.Single(resp.Data!.Lst_Mst_NotifyUpdVersion!);
            Assert.Equal("0", item.FlagNewVersion);
            Assert.Equal("0", item.FlagForcedUpdate);
            Assert.Equal("0", item.FlagStrongForcedUpdate);
            Assert.Null(item.NotifyContent);
            Assert.Null(item.LinkUpdUrl);
        }
    }

    [Fact]
    public async Task GetNotifyUpdVersion_NetworkIdCaseInsensitive_ReturnsMatched()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_Mst_NotifyUpdVersion_Get(new InBrandMstNotifyUpdVersionGetReq
            {
                NetworkID = "net_test_01",
                AppVersion = "1.5.0"
            }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstNotifyUpdVersionGetResp>(okRes.Value);
            Assert.True(resp.Success);
            var item = Assert.Single(resp.Data!.Lst_Mst_NotifyUpdVersion!);
            Assert.Equal("1", item.FlagNewVersion);
            Assert.Equal("0", item.FlagForcedUpdate);
        }
    }

    [Fact]
    public async Task GetNotifyUpdVersion_HeaderInputs_ReturnsExpected()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            controller.ControllerContext.HttpContext.Request.Headers["networkId"] = "NET_TEST_02";
            controller.ControllerContext.HttpContext.Request.Headers["AppVer"] = "2.0.0";
            // NET_TEST_02: MinVersion="2.3.4", DefaultVersion="3.0.0" -> AppVersion="2.0.0" < MinVersion -> FORCEDUPD
            var res = await controller.WA_Mst_NotifyUpdVersion_Get(null, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<InBrandMstNotifyUpdVersionGetResp>(okRes.Value);
            Assert.True(resp.Success);
            var item = Assert.Single(resp.Data!.Lst_Mst_NotifyUpdVersion!);
            Assert.Equal("1", item.FlagNewVersion);
            Assert.Equal("1", item.FlagForcedUpdate);
        }
    }

    [Fact]
    public async Task GetNotifyUpdVersion_RealServer_OnPort8557_RoundTripAndFilters()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), $"test_port_{port}.db");
        if (File.Exists(dbPath)) File.Delete(dbPath);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
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
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

            // Ca 1: Body rong {}
            var res1 = await client.PostAsync("/api/inbrand/wa_mst_notifyupdversion_get", new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
            var json1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(json1).RootElement;
            Assert.True(doc1.GetProperty("Success").GetBoolean());
            Assert.Equal(0, doc1.GetProperty("Data").GetProperty("Lst_Mst_NotifyUpdVersion").GetArrayLength());

            // Ca 2: Loc NetworkID = "NET_TEST_01", AppVersion = "0.9.0" -> FORCEDUPD
            var res2 = await client.PostAsync("/api/inbrand/wa_mst_notifyupdversion_get",
                new StringContent("{\"NetworkID\":\"NET_TEST_01\",\"AppVersion\":\"0.9.0\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
            var json2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(json2).RootElement;
            Assert.True(doc2.GetProperty("Success").GetBoolean());
            var list2 = doc2.GetProperty("Data").GetProperty("Lst_Mst_NotifyUpdVersion");
            Assert.Equal(1, list2.GetArrayLength());
            Assert.Equal("1", list2[0].GetProperty("FlagNewVersion").GetString());
            Assert.Equal("1", list2[0].GetProperty("FlagForcedUpdate").GetString());
            Assert.Equal("1", list2[0].GetProperty("FlagStrongForcedUpdate").GetString());
            Assert.Equal("Phiên bản ứng dụng đã quá cũ, vui lòng cập nhật lên bản mới nhất.", list2[0].GetProperty("NotifyContent").GetString());

            // Ca 3: Loc NetworkID = "NET_TEST_01", AppVersion = "1.5.0" -> OPTIONALUPD (bo loc khac ra gia tri khac)
            var res3 = await client.PostAsync("/api/inbrand/wa_mst_notifyupdversion_get",
                new StringContent("{\"NetworkID\":\"NET_TEST_01\",\"AppVersion\":\"1.5.0\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res3.StatusCode);
            var json3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(json3).RootElement;
            Assert.True(doc3.GetProperty("Success").GetBoolean());
            var list3 = doc3.GetProperty("Data").GetProperty("Lst_Mst_NotifyUpdVersion");
            Assert.Equal(1, list3.GetArrayLength());
            Assert.Equal("1", list3[0].GetProperty("FlagNewVersion").GetString());
            Assert.Equal("0", list3[0].GetProperty("FlagForcedUpdate").GetString());
            Assert.Equal("0", list3[0].GetProperty("FlagStrongForcedUpdate").GetString());
            Assert.Equal("Đã có phiên bản mới, vui lòng cập nhật để trải nghiệm tính năng mới.", list3[0].GetProperty("NotifyContent").GetString());

            // Ca 4: Header networkId va AppVer
            var req4 = new HttpRequestMessage(HttpMethod.Post, "/api/inbrand/wa_mst_notifyupdversion_get")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            req4.Headers.Add("networkId", "NET_TEST_01");
            req4.Headers.Add("AppVer", "1.5.0");
            var res4 = await client.SendAsync(req4);
            Assert.Equal(HttpStatusCode.OK, res4.StatusCode);
            var json4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(json4).RootElement;
            Assert.True(doc4.GetProperty("Success").GetBoolean());
            var list4 = doc4.GetProperty("Data").GetProperty("Lst_Mst_NotifyUpdVersion");
            Assert.Equal(1, list4.GetArrayLength());
            Assert.Equal("1", list4[0].GetProperty("FlagNewVersion").GetString());
            Assert.Equal("0", list4[0].GetProperty("FlagForcedUpdate").GetString());

            // Ca 5: Round-trip: Them moi 1 ban ghi Network moi vao DB va doc lai qua endpoint
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Set<MstSv_Mst_Network>().Add(new MstSv_Mst_Network
                {
                    NetworkID = "NET_ROUND_TRIP",
                    NetworkName = "Mạng round trip",
                    MinVersion = "4.0.0",
                    DefaultVersion = "5.0.0",
                    FlagActive = "1"
                });
                await db.SaveChangesAsync();
            }

            var res5 = await client.PostAsync("/api/inbrand/wa_mst_notifyupdversion_get",
                new StringContent("{\"NetworkID\":\"NET_ROUND_TRIP\",\"AppVersion\":\"3.0.0\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res5.StatusCode);
            var json5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(json5).RootElement;
            Assert.True(doc5.GetProperty("Success").GetBoolean());
            var list5 = doc5.GetProperty("Data").GetProperty("Lst_Mst_NotifyUpdVersion");
            Assert.Equal(1, list5.GetArrayLength());
            Assert.Equal("1", list5[0].GetProperty("FlagNewVersion").GetString());
            Assert.Equal("1", list5[0].GetProperty("FlagForcedUpdate").GetString());
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
