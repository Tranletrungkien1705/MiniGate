using Microsoft.EntityFrameworkCore;
using MiniGate.Data;
using MiniGate.Models;
using MiniGate.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=minigate.db";
builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (DbUtil.IsPostgres(conn)) o.UseNpgsql(DbUtil.ToNpgsql(conn));
    else o.UseSqlite(conn);
});
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IGateAdminService, GateAdminService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<RateLimiter>();
builder.Services.AddSingleton<GatewayProxy>();
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

// ─── CỔNG API: chuyển tiếp /gw/{prefix}/... tới upstream (proxy tự resolve client/org) ───
app.Map("/gw/{**path}", async (HttpContext ctx, string? path, GatewayProxy proxy) =>
{
    await proxy.InvokeAsync(ctx, path ?? "");
});

// Multi-tenant cho ADMIN UI: org từ cookie org_key / header (không dùng cho /gw).
app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Path.StartsWithSegments("/gw"))
    {
        var key = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key)) ctx.Request.Cookies.TryGetValue(TenantContext.CookieName, out key);
        if (!string.IsNullOrWhiteSpace(key))
        {
            using var lookup = app.Services.CreateScope();
            var ldb = lookup.ServiceProvider.GetRequiredService<AppDbContext>();
            var org = await ldb.Orgs.FirstOrDefaultAsync(o => o.ApiKey == key);
            if (org != null) ctx.RequestServices.GetRequiredService<ITenantContext>().OrgId = org.Id;
        }
    }
    await next();
});

app.UseStaticFiles();
app.MapGet("/healthz", () => "ok");

app.MapPost("/api/orgs/register", async (RegisterOrgDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần Name." });
    var org = new Org { Name = dto.Name.Trim(), ApiKey = "gate_" + Guid.NewGuid().ToString("N") };
    db.Orgs.Add(org); await db.SaveChangesAsync();
    return Results.Ok(new { orgId = org.Id, apiKey = org.ApiKey });
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

record RegisterOrgDto(string Name);
