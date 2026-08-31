using Microsoft.EntityFrameworkCore;
using MiniGate.Models;

namespace MiniGate.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await MigratePostgresAsync(db);

        if (!await db.Orgs.AnyAsync(o => o.Id == TenantContext.DefaultOrgId))
        {
            db.Orgs.Add(new Org { Id = TenantContext.DefaultOrgId, Name = "Demo Gate", ApiKey = TenantContext.DefaultApiKey });
            await db.SaveChangesAsync();
        }
        // Tuyến trỏ tới CHÍNH các dịch vụ fleet. UPSERT theo Prefix (idempotent): thêm tuyến thiếu +
        // cập nhật URL khi app relocate — chạy MỖI boot nên đúng kể cả khi DB là SQLite ephemeral.
        var desired = new (string Name, string Prefix, string Url, bool Auth)[]
        {
            ("Identity (SSO/OIDC)", "sso", "https://minisso.onrender.com", false),
            ("Product Center (PIM)", "pim", "https://minipim.onrender.com", false),
            ("Warehouse (WMS)", "wms", "https://miniwms.onrender.com", false),
            ("E-Invoice", "invoice", "https://qinvoicelite.onrender.com", false),
            ("Anti-counterfeit Stamp", "stamp", "https://ministamp.onrender.com", false),
            ("Traceability (nguồn gốc)", "trace", "https://minitrace.onrender.com", false),
            ("Car Service (RO)", "service", "https://miniservice-hytf.onrender.com", false),
            ("Showroom (bán xe)", "showroom", "https://minishowroom.onrender.com", false),
            ("Insurance (bảo hiểm)", "insurance", "https://miniinsurance.onrender.com", false),
            ("Contract (bảo mật)", "contract", "https://minicontract-512u.onrender.com", true),
            ("HR (nhân sự)", "hr", "https://minihr-kz1i.onrender.com", false),
            ("Reconcile (đối soát công nợ)", "reconcile", "https://minireconcile.onrender.com", false),
            ("Promo (khuyến mãi/quay thưởng)", "promo", "https://minipromo-zoq3.onrender.com", false),
            ("Origin (truy xuất GS1 theo lô)", "origin", "https://miniorigin-huug.onrender.com", false),
            ("T-VAN (truyền HĐĐT tới TCT)", "tvan", "https://minitvan-tk.onrender.com", false),
            ("Sign (cổng ký số RSA)", "sign", "https://minisign-9tjk.onrender.com", false),
            ("Geo (danh mục hành chính 2025)", "geo", "https://minigeo-fz3y.onrender.com", false),
            ("Notify (thông báo đa kênh)", "notify", "https://mininotify-yw4g.onrender.com", false),
            ("Loyalty (hội viên/điểm)", "loyalty", "https://miniloyalty-pj9w.onrender.com", false),
            ("CSKH (chăm sóc khách hàng)", "cskh", "https://minicskh-4pir.onrender.com", false),
            ("Payment (VNPay/MoMo)", "pay", "https://minipay-mje6.onrender.com", false),
            ("Vehicle (sổ đăng ký xe VIN)", "vehicle", "https://minivehicle.onrender.com", false),
            ("Parts (phụ tùng + đặt hàng ĐL)", "parts", "https://miniparts.onrender.com", false),
            ("Booking (đặt lịch dịch vụ)", "booking", "https://minibooking.onrender.com", false),
        };
        var existing = await db.Routes.ToDictionaryAsync(r => r.Prefix);
        var changed = false;
        foreach (var d in desired)
        {
            if (existing.TryGetValue(d.Prefix, out var r))
            {
                if (r.UpstreamBaseUrl != d.Url) { r.UpstreamBaseUrl = d.Url; changed = true; }   // app relocate
            }
            else
            {
                db.Routes.Add(new GwRoute { Name = d.Name, Prefix = d.Prefix, UpstreamBaseUrl = d.Url, RequireAuth = d.Auth, IsActive = true });
                changed = true;
            }
        }
        if (changed) await db.SaveChangesAsync();
        if (!await db.Clients.AnyAsync())
        {
            db.Clients.Add(new ApiClient { Name = "Mobile App Demo", ApiKey = "gk_demo_mobile", RateLimitPerMin = 60 });
            await db.SaveChangesAsync();
        }
    }

    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var tables = new[] { "Routes", "Clients", "Logs" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS minigate.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON minigate.\"Orgs\" (\"ApiKey\")",
        };
        foreach (var t in tables) sql.Add($"ALTER TABLE minigate.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}
