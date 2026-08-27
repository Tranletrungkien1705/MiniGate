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
        // Tuyến trỏ tới CHÍNH các dịch vụ trong hệ sinh thái _labs (cổng đứng trước cả fleet).
        if (!await db.Routes.AnyAsync())
        {
            db.Routes.AddRange(
                new GwRoute { Name = "Identity (SSO/OIDC)", Prefix = "sso", UpstreamBaseUrl = "https://minisso.onrender.com" },
                new GwRoute { Name = "Product Center (PIM)", Prefix = "pim", UpstreamBaseUrl = "https://minipim.onrender.com" },
                new GwRoute { Name = "Warehouse (WMS)", Prefix = "wms", UpstreamBaseUrl = "https://miniwms.onrender.com" },
                new GwRoute { Name = "E-Invoice", Prefix = "invoice", UpstreamBaseUrl = "https://qinvoicelite.onrender.com" },
                new GwRoute { Name = "Anti-counterfeit Stamp", Prefix = "stamp", UpstreamBaseUrl = "https://ministamp.onrender.com" },
                new GwRoute { Name = "Traceability (nguồn gốc)", Prefix = "trace", UpstreamBaseUrl = "https://minitrace.onrender.com" },
                new GwRoute { Name = "Car Service (RO)", Prefix = "service", UpstreamBaseUrl = "https://miniservice-hytf.onrender.com" },
                new GwRoute { Name = "Showroom (bán xe)", Prefix = "showroom", UpstreamBaseUrl = "https://minishowroom.onrender.com" },
                new GwRoute { Name = "Insurance (bảo hiểm)", Prefix = "insurance", UpstreamBaseUrl = "https://miniinsurance.onrender.com" },
                new GwRoute { Name = "Contract (bảo mật)", Prefix = "contract", UpstreamBaseUrl = "https://minicontract.onrender.com", RequireAuth = true },
                new GwRoute { Name = "HR (nhân sự)", Prefix = "hr", UpstreamBaseUrl = "https://minihr-kz1i.onrender.com" },
                new GwRoute { Name = "Reconcile (đối soát công nợ)", Prefix = "reconcile", UpstreamBaseUrl = "https://minireconcile.onrender.com" },
                new GwRoute { Name = "Promo (khuyến mãi/quay thưởng)", Prefix = "promo", UpstreamBaseUrl = "https://minipromo.onrender.com" });
            await db.SaveChangesAsync();
        }
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
