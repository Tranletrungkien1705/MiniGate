using Microsoft.EntityFrameworkCore;
using MiniGate.Data;
using MiniGate.Models;
using MiniGate.Services;
using Serilog;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
FleetObs.ConfigureLogger("minigate");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
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
builder.Services.AddFleetObs();
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

app.UseFleetObs();

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

// ─── BI dashboard xuyên app: gom nhật ký cổng (RequestLog) — nguồn số liệu trung tâm cả fleet ───
// Đọc xuyên tenant (IgnoreQueryFilters): traffic/lỗi/độ trễ theo từng app + theo ngày + top tuyến.
app.MapGet("/api/bi/summary", async (AppDbContext db, int? days) =>
{
    var d = Math.Clamp(days ?? 7, 1, 90);
    var from = DateTime.Now.Date.AddDays(-(d - 1));
    var q = db.Logs.IgnoreQueryFilters().Where(l => l.At >= from);

    var total = await q.CountAsync();
    var errors = await q.CountAsync(l => l.StatusCode >= 400);
    var serverErrors = await q.CountAsync(l => l.StatusCode >= 500);
    var avgLatency = total == 0 ? 0 : await q.AverageAsync(l => (double)l.LatencyMs);

    var perApp = await q.GroupBy(l => l.RouteName)
        .Select(g => new
        {
            app = g.Key,
            count = g.Count(),
            errors = g.Count(x => x.StatusCode >= 400),
            avgLatencyMs = Math.Round(g.Average(x => (double)x.LatencyMs), 1),
            maxLatencyMs = g.Max(x => x.LatencyMs),
            lastAt = g.Max(x => x.At)
        })
        .OrderByDescending(x => x.count).ToListAsync();

    var perDayRaw = await q.GroupBy(l => l.At.Date)
        .Select(g => new { day = g.Key, count = g.Count(), errors = g.Count(x => x.StatusCode >= 400) })
        .OrderBy(x => x.day).ToListAsync();
    var perDay = perDayRaw.Select(x => new { day = x.day.ToString("yyyy-MM-dd"), x.count, x.errors });

    var perStatus = (await q.GroupBy(l => l.StatusCode / 100)
        .Select(g => new { bucket = g.Key, count = g.Count() }).ToListAsync())
        .OrderBy(x => x.bucket).Select(x => new { klass = x.bucket + "xx", x.count });

    return Results.Ok(new
    {
        windowDays = d, generatedAt = DateTime.Now,
        totals = new { requests = total, errors, serverErrors,
            errorRatePct = total == 0 ? 0 : Math.Round(errors * 100.0 / total, 2),
            avgLatencyMs = Math.Round(avgLatency, 1) },
        perApp, perStatus, perDay
    });
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

record RegisterOrgDto(string Name);
