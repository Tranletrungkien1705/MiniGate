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
                new SysUser { UserCode = "admin", UserName = "Quản trị hệ thống", UserNick = "admin", BankCode = "VCB", MST = "0101234567", FlagSysAdmin = true },
                new SysUser { UserCode = "nnt_a", UserName = "Kế toán Demo A", UserNick = "ketoan_a", BankCode = "VCB", MST = "0101234567", FlagSysAdmin = false },
                new SysUser { UserCode = "nnt_b", UserName = "Kế toán Demo B", UserNick = "ketoan_b", BankCode = "TCB", MST = "0107654321", FlagSysAdmin = false, FlagDLAdmin = true });
            await db.SaveChangesAsync();
        }
        // Nhóm quyền (Sys_Group) + thành viên nhóm (RptSv_Sys_UserInGroup) cho danh sách người dùng.
        if (!await db.SysGroups.AnyAsync())
        {
            db.SysGroups.AddRange(
                new SysGroup { GroupCode = "GRP_ADMIN", GroupName = "Quản trị hệ thống" },
                new SysGroup { GroupCode = "GRP_KT", GroupName = "Kế toán" },
                new SysGroup { GroupCode = "GRP_DL", GroupName = "Đại lý" });
            await db.SaveChangesAsync();
        }
        if (!await db.SysUserInGroups.AnyAsync())
        {
            db.SysUserInGroups.AddRange(
                new SysUserInGroup { UserCode = "admin", GroupCode = "GRP_ADMIN" },
                new SysUserInGroup { UserCode = "nnt_a", GroupCode = "GRP_KT" },
                new SysUserInGroup { UserCode = "nnt_b", GroupCode = "GRP_KT" },
                new SysUserInGroup { UserCode = "nnt_b", GroupCode = "GRP_DL" });
            await db.SaveChangesAsync();
        }
        // Nhóm mẫu hóa đơn (Invoice_TempGroup) + trường tùy biến (Invoice_TempGroupField) cho danh sách nhóm mẫu.
        if (!await db.InvoiceTempGroups.AnyAsync())
        {
            db.InvoiceTempGroups.AddRange(
                new InvoiceTempGroup { InvoiceTGroupCode = "ITG.001", InvoiceTGroupName = "Nhóm mẫu GTGT cơ bản", MST = "0101234567", InvoiceTGroupBody = "Mẫu hóa đơn GTGT tiêu chuẩn", Spec_Prd_Type = "NORMAL" },
                new InvoiceTempGroup { InvoiceTGroupCode = "ITG.002", InvoiceTGroupName = "Nhóm mẫu bán xe", MST = "0101234567", InvoiceTGroupBody = "Mẫu hóa đơn cho showroom ô tô", Spec_Prd_Type = "CAR" },
                new InvoiceTempGroup { InvoiceTGroupCode = "ITG.003", InvoiceTGroupName = "Nhóm mẫu đại lý B", MST = "0107654321", InvoiceTGroupBody = "Mẫu hóa đơn cho đại lý", Spec_Prd_Type = "DEALER", FlagActive = false });
            await db.SaveChangesAsync();
        }
        if (!await db.InvoiceTempGroupFields.AnyAsync())
        {
            db.InvoiceTempGroupFields.AddRange(
                new InvoiceTempGroupField { InvoiceTGroupCode = "ITG.001", DBFieldName = "BuyerName", NetworkId = "NET-DEMO", TCFType = "TEXT" },
                new InvoiceTempGroupField { InvoiceTGroupCode = "ITG.001", DBFieldName = "BuyerTaxCode", NetworkId = "NET-DEMO", TCFType = "TEXT" },
                new InvoiceTempGroupField { InvoiceTGroupCode = "ITG.002", DBFieldName = "CarPlate", NetworkId = "NET-DEMO", TCFType = "TEXT" },
                new InvoiceTempGroupField { InvoiceTGroupCode = "ITG.002", DBFieldName = "CarEngineNo", NetworkId = "NET-DEMO", TCFType = "TEXT" },
                new InvoiceTempGroupField { InvoiceTGroupCode = "ITG.003", DBFieldName = "DealerCode", NetworkId = "NET-DEMO", TCFType = "TEXT", FlagActive = false });
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
