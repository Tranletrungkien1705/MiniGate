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
using Xunit;

namespace MiniGate.Tests;

// Hạng mục #483 — INOSController/GetUserPermissions (port 1:1).
// Nguồn: INOSController.cs:655-776 -> OS.Inos.cs:4067-4156 (WAS_) -> OS.Inos.cs:4005-4064 (Inos_Permissions_GetUserPermissions).
// Toàn bộ dữ liệu nằm ở dịch vụ ngoài iNOS (UserService.GetUserPermissions) -> GAP, không mô phỏng.
public class INOSGetUserPermissionsTests
{
    private static INOSControllerPort CreateController(string? authorization, string? orgId)
    {
        var http = new DefaultHttpContext();
        if (authorization != null) http.Request.Headers["Authorization"] = authorization;
        if (orgId != null) http.Request.Headers["orgId"] = orgId;
        return new INOSControllerPort
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Fact]
    public void GetUserPermissions_NoOrgId_ReturnsAuthorizeFalse()
    {
        var controller = CreateController("Bearer TOKEN", null);
        var res = controller.GetUserPermissions(new INOSGetUserPermissionsReq { Tid = "TID_1" });

        var okRes = Assert.IsType<OkObjectResult>(res);
        var json = JsonSerializer.Serialize(okRes.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Authorize is false", doc.RootElement.GetProperty("Message").GetString());
        Assert.Equal("Authorize is false", doc.RootElement.GetProperty("Detail").GetString());
    }

    [Fact]
    public void GetUserPermissions_NoToken_ReturnsAuthorizeFalse()
    {
        var controller = CreateController(null, "1001");
        var res = controller.GetUserPermissions(new INOSGetUserPermissionsReq { Tid = "TID_2" });

        var okRes = Assert.IsType<OkObjectResult>(res);
        var json = JsonSerializer.Serialize(okRes.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Authorize is false", doc.RootElement.GetProperty("Message").GetString());
    }

    [Fact]
    public void GetUserPermissions_Authorized_ExternalServiceGap_ReturnsDefaultBizError()
    {
        var controller = CreateController("Bearer TOKEN", "1001");
        var res = controller.GetUserPermissions(new INOSGetUserPermissionsReq
        {
            Tid = "TID_3",
            WAUserCode = "U01",
            UserId = "U01",
            Inos_OrgId = "1001"
        });

        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var json = JsonSerializer.Serialize(bad.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("ErridN.MobileGate.WAS_OS_Inos_OrderService_GetDiscountCode",
            doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("TOKEN", doc.RootElement.GetProperty("Params").GetProperty("Check.strAccessToken").GetString());
    }

    [Fact]
    public async Task GetUserPermissions_RealServer_OnFreePort_GuardAndGap()
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
        }).AddApplicationPart(typeof(INOSControllerPort).Assembly);

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

            // Ca 1: body rỗng, không header -> CheckAuthorize false (INOSController.cs:703-709)
            var res1 = await client.PostAsync("/api/inos/getuserpermissions",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
            var doc1 = JsonDocument.Parse(await res1.Content.ReadAsStringAsync()).RootElement;
            Assert.False(doc1.GetProperty("Success").GetBoolean());
            Assert.Equal("Authorize is false", doc1.GetProperty("Message").GetString());

            // Ca 2: có orgId nhưng thiếu Authorization -> CheckAuthorize false
            var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/inos/getuserpermissions")
            {
                Content = new StringContent("{\"Tid\":\"T2\"}", Encoding.UTF8, "application/json")
            };
            req2.Headers.Add("orgId", "1001");
            var res2 = await client.SendAsync(req2);
            Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
            var doc2 = JsonDocument.Parse(await res2.Content.ReadAsStringAsync()).RootElement;
            Assert.False(doc2.GetProperty("Success").GetBoolean());
            Assert.Equal("Authorize is false", doc2.GetProperty("Detail").GetString());

            // Ca 3: đủ header -> qua guard, dịch vụ ngoài iNOS GAP -> 400 mã lỗi mặc định của biz
            var req3 = new HttpRequestMessage(HttpMethod.Post, "/api/inos/getuserpermissions")
            {
                Content = new StringContent("{\"Tid\":\"T3\",\"WAUserCode\":\"U01\"}", Encoding.UTF8, "application/json")
            };
            req3.Headers.Add("orgId", "1001");
            req3.Headers.Add("Authorization", "Bearer TOKEN");
            var res3 = await client.SendAsync(req3);
            Assert.Equal(HttpStatusCode.BadRequest, res3.StatusCode);
            var doc3 = JsonDocument.Parse(await res3.Content.ReadAsStringAsync()).RootElement;
            Assert.False(doc3.GetProperty("Success").GetBoolean());
            Assert.Equal("ErridN.MobileGate.WAS_OS_Inos_OrderService_GetDiscountCode",
                doc3.GetProperty("ErrorCode").GetString());
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
