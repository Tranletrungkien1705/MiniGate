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
                new GwRoute { Name = "Product Center (PIM)", Prefix = "pim", UpstreamBaseUrl = "https://minipim.onrender.com" },
                new GwRoute { Name = "Warehouse (WMS)", Prefix = "wms", UpstreamBaseUrl = "https://miniwms.onrender.com" },
                new GwRoute { Name = "E-Invoice", Prefix = "invoice", UpstreamBaseUrl = "https://qinvoicelite.onrender.com" },
                new GwRoute { Name = "Anti-counterfeit Stamp", Prefix = "stamp", UpstreamBaseUrl = "https://ministamp.onrender.com" },
                new GwRoute { Name = "Car Service (RO)", Prefix = "service", UpstreamBaseUrl = "https://miniservice-hytf.onrender.com" },
                new GwRoute { Name = "Showroom (bán xe)", Prefix = "showroom", UpstreamBaseUrl = "https://minishowroom.onrender.com" },
                new GwRoute { Name = "Contract (bảo mật)", Prefix = "contract", UpstreamBaseUrl = "https://minicontract.onrender.com", RequireAuth = true });
            await db.SaveChangesAsync();
        }
        if (!await db.Clients.AnyAsync())
        {
            db.Clients.Add(new ApiClient { Name = "Mobile App Demo", ApiKey = "gk_demo_mobile", RateLimitPerMin = 60 });
            await db.SaveChangesAsync();
        }
        // Dữ liệu mẫu cho báo cáo tình hình sử dụng hóa đơn (BC26/AC).
        if (!await db.InvoiceTemplates.AnyAsync())
        {
            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var t1 = new InvoiceTemplate { TInvoiceCode = "TINVOICODE.001", TInvoiceName = "Hóa đơn GTGT 1/001", InvoiceType = "01GTKT", FormNo = "1/001", Sign = "AA/19E", MST = "0101234567", StartInvoiceNo = 1, EndInvoiceNo = 500, EffDateStart = monthStart };
            var t2 = new InvoiceTemplate { TInvoiceCode = "TINVOICODE.002", TInvoiceName = "Hóa đơn GTGT 2/001", InvoiceType = "01GTKT", FormNo = "2/001", Sign = "AA/19E", MST = "0101234567", StartInvoiceNo = 1, EndInvoiceNo = 200, EffDateStart = monthStart };
            db.InvoiceTemplates.AddRange(t1, t2);
            await db.SaveChangesAsync();

            var invs = new List<Invoice>();
            for (long n = 1; n <= 120; n++)
                invs.Add(new Invoice { TInvoiceCode = t1.TInvoiceCode, InvoiceNo = n, InvoiceDate = monthStart.AddDays(2), InvoiceStatus = "ISSUED", MST = t1.MST, AmountAfterVAT = 1_100_000m, CreatedAt = monthStart.AddDays(2) });
            for (long n = 121; n <= 125; n++)
                invs.Add(new Invoice { TInvoiceCode = t1.TInvoiceCode, InvoiceNo = n, InvoiceDate = monthStart.AddDays(3), InvoiceStatus = "DELETED", MST = t1.MST, AmountAfterVAT = 550_000m, CreatedAt = monthStart.AddDays(3) });
            for (long n = 1; n <= 40; n++)
                invs.Add(new Invoice { TInvoiceCode = t2.TInvoiceCode, InvoiceNo = n, InvoiceDate = monthStart.AddDays(4), InvoiceStatus = "ISSUED", MST = t2.MST, AmountAfterVAT = 2_200_000m, CreatedAt = monthStart.AddDays(4) });
            db.Invoices.AddRange(invs);
            await db.SaveChangesAsync();
        }
        // Hạn mức phát hành theo MST (Invoice_license) cho bảng điều khiển hóa đơn.
        if (!await db.InvoiceLicenses.AnyAsync())
        {
            db.InvoiceLicenses.Add(new InvoiceLicense { MST = "0101234567", NetworkId = "NET-DEMO", TotalQty = 1000, TotalQtyIssued = 700, TotalQtyUsed = 160, TotalQtyCancel = 5 });
            await db.SaveChangesAsync();
        }
        // Danh mục NNT (Mst_NNT) + user (Sys_User) cho phân quyền xem theo MST (Mst_NNT_ViewAbility).
        if (!await db.MstNnts.AnyAsync())
        {
            db.MstNnts.AddRange(
                new MstNnt { MST = "0101234567", NNTFullName = "Công ty Demo A", NNTAddress = "Hà Nội", MSTBUPattern = "ALL.0101234567%" },
                new MstNnt { MST = "0107654321", NNTFullName = "Công ty Demo B", NNTAddress = "TP.HCM", MSTBUPattern = "ALL.0107654321%" });
            await db.SaveChangesAsync();
        }
        if (!await db.SysUsers.AnyAsync())
        {
            db.SysUsers.AddRange(
                new SysUser { UserCode = "admin", UserName = "Quản trị hệ thống", MST = "0101234567", FlagSysAdmin = true },
                new SysUser { UserCode = "nnt_a", UserName = "Kế toán Demo A", MST = "0101234567", FlagSysAdmin = false });
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
