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

public class EcoreInosCheckUserExistTests
{
    private static async Task<(AppDbContext db, EcoreControllerPort controller, SqliteConnection conn)> CreateTestContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        await db.Database.EnsureCreatedAsync();
        await PortSchema.EnsureAsync(db);

        var controller = new EcoreControllerPort
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return (db, controller, conn);
    }

    private static JsonElement ToJsonElement(object? value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public async Task InosCheckUserExist_NullReq_ReturnsFail_WAS_Inos_GetUser()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.Inos_CheckUserExist(null, db);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            var el = ToJsonElement(badReq.Value);
            Assert.False(el.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.WAS_Inos_GetUser", el.GetProperty("ErrorCode").GetString());
        }
    }

    [Fact]
    public async Task InosCheckUserExist_EmptyUserCode_ReturnsFail_WAS_Inos_GetUser()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.Inos_CheckUserExist(new EcoreInosCheckUserExistReq { UserCode = "   " }, db);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            var el = ToJsonElement(badReq.Value);
            Assert.False(el.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.WAS_Inos_GetUser", el.GetProperty("ErrorCode").GetString());
        }
    }

    [Fact]
    public async Task InosCheckUserExist_InvalidFormatUserCode_ReturnsFail_WAS_Inos_GetUser()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            // Không phải email (không có @), không phải phone 10 chữ số, không chuyển đổi được sang double
            var res = await controller.Inos_CheckUserExist(new EcoreInosCheckUserExistReq { UserCode = "invalid_user_code" }, db);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            var el = ToJsonElement(badReq.Value);
            Assert.False(el.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.WAS_Inos_GetUser", el.GetProperty("ErrorCode").GetString());
        }
    }

    [Fact]
    public async Task InosCheckUserExist_UserInBlackList_Email_ReturnsFail_UserCodeDeleted()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            // blocked@idocnet.com đã được seeder khởi tạo
            var res = await controller.Inos_CheckUserExist(new EcoreInosCheckUserExistReq { UserCode = "blocked@idocnet.com" }, db);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            var el = ToJsonElement(badReq.Value);
            Assert.False(el.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted", el.GetProperty("ErrorCode").GetString());
            Assert.Equal("blocked@idocnet.com", el.GetProperty("Params").GetProperty("Check.UserCode").GetString());
        }
    }

    [Fact]
    public async Task InosCheckUserExist_UserInBlackList_Phone_ReturnsFail_UserCodeDeleted()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            // 0912345678 đã được seeder khởi tạo
            var res = await controller.Inos_CheckUserExist(new EcoreInosCheckUserExistReq { UserCode = "0912345678" }, db);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            var el = ToJsonElement(badReq.Value);
            Assert.False(el.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted", el.GetProperty("ErrorCode").GetString());
        }
    }

    [Fact]
    public async Task InosCheckUserExist_UserInBlackList_NumericId_ReturnsFail_UserCodeDeleted()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            // 999999 đã được seeder khởi tạo
            var res = await controller.Inos_CheckUserExist(new EcoreInosCheckUserExistReq { UserCode = "999999" }, db);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            var el = ToJsonElement(badReq.Value);
            Assert.False(el.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted", el.GetProperty("ErrorCode").GetString());
        }
    }

    [Fact]
    public async Task InosCheckUserExist_UserInBlackList_CaseInsensitive_ReturnsFail_UserCodeDeleted()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            // BLOCKED@IDOCNET.COM khớp case-insensitive với blocked@idocnet.com
            var res = await controller.Inos_CheckUserExist(new EcoreInosCheckUserExistReq { UserCode = "BLOCKED@IDOCNET.COM" }, db);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            var el = ToJsonElement(badReq.Value);
            Assert.False(el.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted", el.GetProperty("ErrorCode").GetString());
        }
    }

    [Fact]
    public async Task InosCheckUserExist_ValidEmailNotInBlackList_ReturnsSuccess()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.Inos_CheckUserExist(new EcoreInosCheckUserExistReq { UserCode = "activeuser@idocnet.com", Tid = "TID_TEST_01" }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<EcoreInosCheckUserExistResp>(okRes.Value);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);
            Assert.Null(resp.Data.InosUser);
            var sysInfo = Assert.Single(resp.Data.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
            Assert.Equal("TID_TEST_01", sysInfo.Tid);
            Assert.Equal("0", sysInfo.ErrorCode);
        }
    }

    [Fact]
    public async Task InosCheckUserExist_ValidPhoneNotInBlackList_ReturnsSuccess()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.Inos_CheckUserExist(new EcoreInosCheckUserExistReq { UserCode = "0988888888" }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<EcoreInosCheckUserExistResp>(okRes.Value);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);
        }
    }

    [Fact]
    public async Task InosCheckUserExist_ValidNumericIdNotInBlackList_ReturnsSuccess()
    {
        var (db, controller, conn) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.Inos_CheckUserExist(new EcoreInosCheckUserExistReq { UserCode = "123456" }, db);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<EcoreInosCheckUserExistResp>(okRes.Value);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);
        }
    }

    [Theory]
    [InlineData("0987654321", true)]
    [InlineData("0912345678", true)]
    [InlineData("090-123-4567", true)]
    [InlineData("090.123.4567", true)]
    [InlineData("090 123 4567", true)]
    [InlineData("(090) 123-4567", true)]
    [InlineData("12345", false)]
    [InlineData("abc", false)]
    [InlineData(null, false)]
    public void PhoneNumberHelper_MatchesMotif(string? phone, bool expected)
    {
        Assert.Equal(expected, PhoneNumberHelper.IsPhoneNbr(phone));
    }

    [Fact]
    public async Task InosCheckUserExist_RealServer_OnPort8550_RoundTripAndGuards()
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "test_port_8550.db");
        if (File.Exists(dbPath)) File.Delete(dbPath);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:8550");
        builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
        builder.Services.AddScoped<ITenantContext, TenantContext>();
        builder.Services.AddControllers(o =>
        {
            o.Filters.Add<PortJsonResultFilter>();
            o.Filters.Add<PortExceptionFilter>();
        }).AddApplicationPart(typeof(EcoreControllerPort).Assembly);

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
            using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8550") };

            // Ca 1: Body rong {}
            var res1 = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, res1.StatusCode);
            var json1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(json1).RootElement;
            Assert.False(doc1.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.WAS_Inos_GetUser", doc1.GetProperty("ErrorCode").GetString());

            // Ca 2: Guard UserCode rong
            var res2 = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent("{\"UserCode\":\"\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, res2.StatusCode);
            var json2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(json2).RootElement;
            Assert.False(doc2.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.WAS_Inos_GetUser", doc2.GetProperty("ErrorCode").GetString());

            // Ca 3: Guard UserCode khong hop le
            var res3 = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent("{\"UserCode\":\"invalid_user_code\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, res3.StatusCode);
            var json3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(json3).RootElement;
            Assert.False(doc3.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.WAS_Inos_GetUser", doc3.GetProperty("ErrorCode").GetString());

            // Ca 4: Guard UserCode trong BlackList (Email: blocked@idocnet.com)
            var res4 = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent("{\"UserCode\":\"blocked@idocnet.com\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, res4.StatusCode);
            var json4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(json4).RootElement;
            Assert.False(doc4.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted", doc4.GetProperty("ErrorCode").GetString());
            Assert.Equal("blocked@idocnet.com", doc4.GetProperty("Params").GetProperty("Check.UserCode").GetString());

            // Ca 5: Guard UserCode trong BlackList (Phone: 0912345678)
            var res5 = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent("{\"UserCode\":\"0912345678\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, res5.StatusCode);
            var json5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(json5).RootElement;
            Assert.Equal("ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted", doc5.GetProperty("ErrorCode").GetString());

            // Ca 6: Guard UserCode trong BlackList (ID so: 999999)
            var res6 = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent("{\"UserCode\":\"999999\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, res6.StatusCode);
            var json6 = await res6.Content.ReadAsStringAsync();
            var doc6 = JsonDocument.Parse(json6).RootElement;
            Assert.Equal("ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted", doc6.GetProperty("ErrorCode").GetString());

            // Ca 7: Thanh cong - Email hop le khong trong BlackList
            var res7 = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent("{\"UserCode\":\"activeuser@idocnet.com\",\"Tid\":\"TID_VERIFY_01\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res7.StatusCode);
            var json7 = await res7.Content.ReadAsStringAsync();
            var doc7 = JsonDocument.Parse(json7).RootElement;
            Assert.True(doc7.GetProperty("Success").GetBoolean());
            Assert.Equal("TID_VERIFY_01", doc7.GetProperty("Data").GetProperty("c_K_DT_Sys").GetProperty("Lst_c_K_DT_SysInfo")[0].GetProperty("Tid").GetString());

            // Ca 8: Thanh cong - Phone hop le khong trong BlackList
            var res8 = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent("{\"UserCode\":\"0988888888\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res8.StatusCode);
            var json8 = await res8.Content.ReadAsStringAsync();
            var doc8 = JsonDocument.Parse(json8).RootElement;
            Assert.True(doc8.GetProperty("Success").GetBoolean());

            // Ca 9: Thanh cong - ID so hop le khong trong BlackList
            var res9 = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent("{\"UserCode\":\"123456\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res9.StatusCode);
            var json9 = await res9.Content.ReadAsStringAsync();
            var doc9 = JsonDocument.Parse(json9).RootElement;
            Assert.True(doc9.GetProperty("Success").GetBoolean());

            // Ca 10: Round-trip: Them moi 1 user vao DB Mst_UserBlackList va kiem tra endpoint ngay lap tuc chan
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Set<Mst_UserBlackList>().Add(new Mst_UserBlackList
                {
                    UserCode = "newlyblocked@idocnet.com",
                    NetworkID = "1001",
                    UserName = "Newly Blocked",
                    EMail = "newlyblocked@idocnet.com",
                    FlagActive = "1"
                });
                await db.SaveChangesAsync();
            }

            var res10 = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent("{\"UserCode\":\"newlyblocked@idocnet.com\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, res10.StatusCode);
            var json10 = await res10.Content.ReadAsStringAsync();
            var doc10 = JsonDocument.Parse(json10).RootElement;
            Assert.False(doc10.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted", doc10.GetProperty("ErrorCode").GetString());
            Assert.Equal("newlyblocked@idocnet.com", doc10.GetProperty("Params").GetProperty("Check.UserCode").GetString());
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
