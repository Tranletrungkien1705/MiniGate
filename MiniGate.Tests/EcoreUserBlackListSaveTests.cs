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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniGate.Controllers.Port;
using MiniGate.Data;
using MiniGate.Models.Port;
using Xunit;

namespace MiniGate.Tests;

public class EcoreUserBlackListSaveTests
{
    private static async Task<(AppDbContext db, EcoreControllerPort controller, SqliteConnection conn, IConfiguration config)> CreateTestContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        await db.Database.EnsureCreatedAsync();
        await PortSchema.EnsureAsync(db);

        var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?>
        {
            {"Biz_NetworkID", "1001"}
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var controller = new EcoreControllerPort
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return (db, controller, conn, config);
    }

    private static JsonElement ToJsonElement(object? value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public async Task UserBlackListSave_NullReq_ReturnsFail_WAS_Mst_UserBlackList_Save()
    {
        var (db, controller, conn, config) = await CreateTestContextAsync();
        await using (conn)
        {
            var res = await controller.WA_Mst_UserBlackList_Save(null, db, config);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            var el = ToJsonElement(badReq.Value);
            Assert.False(el.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.WAS_Mst_UserBlackList_Save", el.GetProperty("ErrorCode").GetString());
        }
    }

    [Fact]
    public async Task UserBlackListSave_NullMstUserBlackList_ReturnsFail_WAS_Mst_UserBlackList_Save()
    {
        var (db, controller, conn, config) = await CreateTestContextAsync();
        await using (conn)
        {
            var req = new EcoreUserBlackListSaveReq { Mst_UserBlackList = null };
            var res = await controller.WA_Mst_UserBlackList_Save(req, db, config);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            var el = ToJsonElement(badReq.Value);
            Assert.False(el.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.WAS_Mst_UserBlackList_Save", el.GetProperty("ErrorCode").GetString());
        }
    }

    [Fact]
    public async Task UserBlackListSave_EmptyUserCodeOnCreate_ReturnsFail_Mst_UserBlackList_Save()
    {
        var (db, controller, conn, config) = await CreateTestContextAsync();
        await using (conn)
        {
            var req = new EcoreUserBlackListSaveReq
            {
                FlagIsDelete = "0",
                Mst_UserBlackList = new Mst_UserBlackListInputDto
                {
                    UserCode = "   ",
                    UserName = "No User Code"
                }
            };
            var res = await controller.WA_Mst_UserBlackList_Save(req, db, config);
            var badReq = Assert.IsType<BadRequestObjectResult>(res);
            var el = ToJsonElement(badReq.Value);
            Assert.False(el.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.Mst_UserBlackList_Save", el.GetProperty("ErrorCode").GetString());
        }
    }

    [Fact]
    public async Task UserBlackListSave_DeleteNonExistentUser_ReturnsSuccess()
    {
        var (db, controller, conn, config) = await CreateTestContextAsync();
        await using (conn)
        {
            var req = new EcoreUserBlackListSaveReq
            {
                FlagIsDelete = "1",
                Tid = "TID_DELETE_01",
                Mst_UserBlackList = new Mst_UserBlackListInputDto
                {
                    UserCode = "nonexistent@idocnet.com"
                }
            };
            var res = await controller.WA_Mst_UserBlackList_Save(req, db, config);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<EcoreUserBlackListSaveResp>(okRes.Value);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);
            Assert.Null(resp.Data.Lst_Mst_UserBlackList);
            var info = Assert.Single(resp.Data.c_K_DT_Sys.Lst_c_K_DT_SysInfo);
            Assert.Equal("TID_DELETE_01", info.Tid);
            Assert.Equal("0", info.ErrorCode);
        }
    }

    [Fact]
    public async Task UserBlackListSave_CreateUser_StoresInDb_WithUppercaseUserCode()
    {
        var (db, controller, conn, config) = await CreateTestContextAsync();
        await using (conn)
        {
            var req = new EcoreUserBlackListSaveReq
            {
                FlagIsDelete = "0",
                WAUserCode = "admin01",
                Tid = "TID_CREATE_01",
                Mst_UserBlackList = new Mst_UserBlackListInputDto
                {
                    UserCode = "newuser@idocnet.com",
                    ProviderCode = "prov01",
                    UserName = "New User Blacklist",
                    PhoneNo = "0987111222",
                    EMail = "newuser@idocnet.com",
                    UUID = "uuid-test-01"
                }
            };

            var res = await controller.WA_Mst_UserBlackList_Save(req, db, config);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<EcoreUserBlackListSaveResp>(okRes.Value);
            Assert.True(resp.Success);

            // Kiểm tra DB: UserCode được StdParam chuẩn hoá thành chữ HOA
            var saved = await db.Set<Mst_UserBlackList>().FirstOrDefaultAsync(x => x.UserCode == "NEWUSER@IDOCNET.COM");
            Assert.NotNull(saved);
            Assert.Equal("NEWUSER@IDOCNET.COM", saved.UserCode);
            Assert.Equal("PROV01", saved.ProviderCode);
            Assert.Equal("New User Blacklist", saved.UserName);
            Assert.Equal("0987111222", saved.PhoneNo);
            Assert.Equal("newuser@idocnet.com", saved.EMail);
            Assert.Equal("uuid-test-01", saved.UUID);
            Assert.Equal("1", saved.FlagActive);
            Assert.Equal("admin01", saved.LogLUBy);
            Assert.Equal("1001", saved.NetworkID);
        }
    }

    [Fact]
    public async Task UserBlackListSave_UpdateExistingUser_ReplacesFields()
    {
        var (db, controller, conn, config) = await CreateTestContextAsync();
        await using (conn)
        {
            // Tạo trước
            var reqCreate = new EcoreUserBlackListSaveReq
            {
                FlagIsDelete = "0",
                Mst_UserBlackList = new Mst_UserBlackListInputDto
                {
                    UserCode = "updateme@idocnet.com",
                    UserName = "Before Update",
                    PhoneNo = "0900000001"
                }
            };
            await controller.WA_Mst_UserBlackList_Save(reqCreate, db, config);

            // Cập nhật
            var reqUpdate = new EcoreUserBlackListSaveReq
            {
                FlagIsDelete = "0",
                WAUserCode = "updater01",
                Mst_UserBlackList = new Mst_UserBlackListInputDto
                {
                    UserCode = "UPDATEME@IDOCNET.COM",
                    UserName = "After Update",
                    PhoneNo = "0900000002"
                }
            };
            var res = await controller.WA_Mst_UserBlackList_Save(reqUpdate, db, config);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<EcoreUserBlackListSaveResp>(okRes.Value);
            Assert.True(resp.Success);

            var updated = await db.Set<Mst_UserBlackList>().FirstOrDefaultAsync(x => x.UserCode == "UPDATEME@IDOCNET.COM");
            Assert.NotNull(updated);
            Assert.Equal("After Update", updated.UserName);
            Assert.Equal("0900000002", updated.PhoneNo);
            Assert.Equal("updater01", updated.LogLUBy);
        }
    }

    [Fact]
    public async Task UserBlackListSave_DeleteExistingUser_RemovesFromDb()
    {
        var (db, controller, conn, config) = await CreateTestContextAsync();
        await using (conn)
        {
            // blocked@idocnet.com có sẵn trong seeder
            var existingBefore = await db.Set<Mst_UserBlackList>().FirstOrDefaultAsync(x => x.UserCode == "blocked@idocnet.com");
            Assert.NotNull(existingBefore);

            var reqDelete = new EcoreUserBlackListSaveReq
            {
                FlagIsDelete = "1",
                Mst_UserBlackList = new Mst_UserBlackListInputDto
                {
                    UserCode = "blocked@idocnet.com"
                }
            };
            var res = await controller.WA_Mst_UserBlackList_Save(reqDelete, db, config);
            var okRes = Assert.IsType<OkObjectResult>(res);
            var resp = Assert.IsType<EcoreUserBlackListSaveResp>(okRes.Value);
            Assert.True(resp.Success);

            var existingAfter = await db.Set<Mst_UserBlackList>().FirstOrDefaultAsync(x => x.UserCode == "blocked@idocnet.com");
            Assert.Null(existingAfter);
        }
    }

    [Fact]
    public async Task UserBlackListSave_RealServer_OnPort8565_RoundTripAndGuards()
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "test_port_8565.db");
        if (File.Exists(dbPath)) File.Delete(dbPath);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:8565");
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
            using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8565") };

            // Ca 1: Body rong {} -> ErridN.MobileGate.WAS_Mst_UserBlackList_Save
            var res1 = await client.PostAsync("/api/ecore/wa_mst_userblacklist_save", new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, res1.StatusCode);
            var json1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(json1).RootElement;
            Assert.False(doc1.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.WAS_Mst_UserBlackList_Save", doc1.GetProperty("ErrorCode").GetString());

            // Ca 2: Guard UserCode rong khi tao moi -> ErridN.MobileGate.Mst_UserBlackList_Save
            var req2 = "{\"FlagIsDelete\":\"0\",\"Mst_UserBlackList\":{\"UserCode\":\"\",\"UserName\":\"Test\"}}";
            var res2 = await client.PostAsync("/api/ecore/wa_mst_userblacklist_save", new StringContent(req2, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, res2.StatusCode);
            var json2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(json2).RootElement;
            Assert.False(doc2.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.Mst_UserBlackList_Save", doc2.GetProperty("ErrorCode").GetString());

            // Ca 3: Xoa ban ghi khong ton tai -> Success: true
            var req3 = "{\"FlagIsDelete\":\"1\",\"Mst_UserBlackList\":{\"UserCode\":\"never_existed@idocnet.com\"}}";
            var res3 = await client.PostAsync("/api/ecore/wa_mst_userblacklist_save", new StringContent(req3, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res3.StatusCode);
            var json3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(json3).RootElement;
            Assert.True(doc3.GetProperty("Success").GetBoolean());

            // Ca 4: Round-trip: Them moi 1 user vao BlackList qua /api/ecore/wa_mst_userblacklist_save
            var req4 = "{\"FlagIsDelete\":\"0\",\"WAUserCode\":\"admin\",\"Mst_UserBlackList\":{\"UserCode\":\"blocked_via_save@idocnet.com\",\"UserName\":\"Save Blocked\",\"PhoneNo\":\"0933444555\",\"EMail\":\"blocked_via_save@idocnet.com\"}}";
            var res4 = await client.PostAsync("/api/ecore/wa_mst_userblacklist_save", new StringContent(req4, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res4.StatusCode);
            var json4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(json4).RootElement;
            Assert.True(doc4.GetProperty("Success").GetBoolean());

            // Kiem tra endpoint inos_checkuserexist chan user nay ngay lap tuc
            var checkReq = "{\"UserCode\":\"blocked_via_save@idocnet.com\"}";
            var checkRes = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent(checkReq, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, checkRes.StatusCode);
            var checkJson = await checkRes.Content.ReadAsStringAsync();
            var checkDoc = JsonDocument.Parse(checkJson).RootElement;
            Assert.False(checkDoc.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted", checkDoc.GetProperty("ErrorCode").GetString());

            // Ca 5: Round-trip: Xoa user nay khoi BlackList qua /api/ecore/wa_mst_userblacklist_save
            var req5 = "{\"FlagIsDelete\":\"1\",\"Mst_UserBlackList\":{\"UserCode\":\"blocked_via_save@idocnet.com\"}}";
            var res5 = await client.PostAsync("/api/ecore/wa_mst_userblacklist_save", new StringContent(req5, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res5.StatusCode);
            var json5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(json5).RootElement;
            Assert.True(doc5.GetProperty("Success").GetBoolean());

            // Kiem tra endpoint inos_checkuserexist gio day thanh cong cho user nay
            var checkRes2 = await client.PostAsync("/api/ecore/inos_checkuserexist", new StringContent(checkReq, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, checkRes2.StatusCode);
            var checkJson2 = await checkRes2.Content.ReadAsStringAsync();
            var checkDoc2 = JsonDocument.Parse(checkJson2).RootElement;
            Assert.True(checkDoc2.GetProperty("Success").GetBoolean());
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
