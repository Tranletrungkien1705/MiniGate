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
        // Đơn hàng license (LicOrder) + hoa hồng (LicOrderCommission) cho báo cáo hoa hồng đơn hàng license.
        if (!await db.LicOrders.AnyAsync())
        {
            var day = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(5);
            db.LicOrders.AddRange(
                new LicOrder { OrderId = 1001, OrgName = "Công ty Demo A", DiscountCode = "DC10", TotalCost = 8_000_000m, Price = 10_000_000m, PaymentCode = "PAY-1001", PaymentStatusDesc = "Đã thanh toán", OrderStatus = "APPROVED", PaymentStatus = "PAID", CreateDTime = day, ApproveDTime = day.AddDays(1), CreateUserId = "admin", Remark = "Đơn gói HDDT 1 năm", InosOrgId = 501, InosNetworkId = 1, MST = "0101234567", DLCode = "DL001" },
                new LicOrder { OrderId = 1002, OrgName = "Công ty Demo B", DiscountCode = "DC05", TotalCost = 4_500_000m, Price = 5_000_000m, PaymentCode = "PAY-1002", PaymentStatusDesc = "Chờ thanh toán", OrderStatus = "PENDING", PaymentStatus = "UNPAID", CreateDTime = day.AddDays(2), CreateUserId = "nnt_a", Remark = "Đơn gói HDDT 6 tháng", InosOrgId = 502, InosNetworkId = 2, MST = "0107654321", DLCode = "DL002" },
                new LicOrder { OrderId = 1003, OrgName = "Công ty Demo A", DiscountCode = "DC15", TotalCost = 17_000_000m, Price = 20_000_000m, PaymentCode = "PAY-1003", PaymentStatusDesc = "Đã thanh toán", OrderStatus = "APPROVED", PaymentStatus = "PAID", CreateDTime = day.AddDays(3), ApproveDTime = day.AddDays(4), CreateUserId = "admin", Remark = "Đơn gói HDDT 2 năm", InosOrgId = 501, InosNetworkId = 1, MST = "0101234567", DLCode = "DL001" });
            await db.SaveChangesAsync();
        }
        if (!await db.LicOrderCommissions.AnyAsync())
        {
            db.LicOrderCommissions.AddRange(
                new LicOrderCommission { OrderId = 1001, CommissionStatus = "PAID", Remark = "Đã chi trả", Presenter1 = "ketoan_a", Telesale = "ketoan_b", Implementer = "admin", CommissionPresenter1 = 500_000m, CommissionTelesale = 300_000m, CommissionImplementer = 200_000m },
                new LicOrderCommission { OrderId = 1002, CommissionStatus = "PENDING", Remark = "Chờ duyệt", Presenter1 = "ketoan_a", Consultants = "ketoan_b", CommissionPresenter1 = 250_000m, CommissionConsultants = 150_000m },
                new LicOrderCommission { OrderId = 1003, CommissionStatus = "APPROVED", Remark = "Đã duyệt chờ trả", Presenter1 = "ketoan_a", Presenter2 = "ketoan_b", Telesale = "ketoan_b", Implementer = "admin", CommissionPresenter1 = 1_000_000m, CommissionPresenter2 = 400_000m, CommissionTelesale = 600_000m, CommissionImplementer = 500_000m });
            await db.SaveChangesAsync();
        }
        // Cấu hình chiết khấu đại lý (Map_DealerDiscount) cho danh sách chiết khấu đại lý.
        if (!await db.MapDealerDiscounts.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.MapDealerDiscounts.AddRange(
                new MapDealerDiscount { DLCode = "DL001", DiscountCode = "DC10", FlagActive = true, LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MapDealerDiscount { DLCode = "DL001", DiscountCode = "DC15", FlagActive = true, LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MapDealerDiscount { DLCode = "DL002", DiscountCode = "DC05", FlagActive = true, LogLUDTimeUTC = upd, LogLUBy = "nnt_a" },
                new MapDealerDiscount { DLCode = "DL003", DiscountCode = "DC20", FlagActive = false, LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
            await db.SaveChangesAsync();
        }
        // Đối tượng/chức năng (RptSv_Sys_Object) + quyền truy cập của nhóm (RptSv_Sys_Access).
        if (!await db.SysObjects.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.SysObjects.AddRange(
                new SysObject { ObjectCode = "OBJ_INVOICE", ObjectName = "Quản lý hóa đơn", ServiceCode = "INVOICE", ObjectType = "SCREEN", FlagExecModal = "0", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysObject { ObjectCode = "OBJ_REPORT", ObjectName = "Báo cáo tổng hợp", ServiceCode = "REPORT", ObjectType = "SCREEN", FlagExecModal = "0", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysObject { ObjectCode = "OBJ_USER", ObjectName = "Quản lý người dùng", ServiceCode = "SYS", ObjectType = "SCREEN", FlagExecModal = "1", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysObject { ObjectCode = "OBJ_DEALER", ObjectName = "Quản lý đại lý", ServiceCode = "SYS", ObjectType = "SCREEN", FlagExecModal = "0", FlagActive = false, LogLUDTimeUTC = upd, LogLUBy = "admin" });
            await db.SaveChangesAsync();
        }
        if (!await db.SysAccesses.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.SysAccesses.AddRange(
                new SysAccess { GroupCode = "GRP_ADMIN", ObjectCode = "OBJ_INVOICE", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysAccess { GroupCode = "GRP_ADMIN", ObjectCode = "OBJ_REPORT", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysAccess { GroupCode = "GRP_ADMIN", ObjectCode = "OBJ_USER", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysAccess { GroupCode = "GRP_KT", ObjectCode = "OBJ_INVOICE", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysAccess { GroupCode = "GRP_KT", ObjectCode = "OBJ_REPORT", LogLUDTimeUTC = upd, LogLUBy = "nnt_a" },
                new SysAccess { GroupCode = "GRP_DL", ObjectCode = "OBJ_DEALER", LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
            await db.SaveChangesAsync();
        }
        // Danh mục thuế suất GTGT (Mst_VATRate) cho danh sách thuế suất.
        if (!await db.MstVatRates.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.MstVatRates.AddRange(
                new MstVatRate { VATRateCode = "VAT00", NetworkID = "NET-DEMO", VATRate = 0m, VATDesc = "Không chịu thuế", Remark = "Hàng hóa không chịu thuế GTGT", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MstVatRate { VATRateCode = "VAT05", NetworkID = "NET-DEMO", VATRate = 5m, VATDesc = "Thuế suất 5%", Remark = "Áp dụng cho một số mặt hàng thiết yếu", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MstVatRate { VATRateCode = "VAT08", NetworkID = "NET-DEMO", VATRate = 8m, VATDesc = "Thuế suất 8%", Remark = "Thuế suất ưu đãi theo Nghị quyết", LogLUDTimeUTC = upd, LogLUBy = "nnt_a" },
                new MstVatRate { VATRateCode = "VAT10", NetworkID = "NET-DEMO", VATRate = 10m, VATDesc = "Thuế suất 10%", Remark = "Thuế suất phổ thông", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MstVatRate { VATRateCode = "VAT20", NetworkID = "NET-DEMO", VATRate = 20m, VATDesc = "Thuế suất 20%", Remark = "Thuế suất đặc biệt", FlagActive = false, LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
            await db.SaveChangesAsync();
        }
        // Đối tượng trong mô-đun (Sys_ObjectInModules) cho danh sách đối tượng trong mô-đun.
        if (!await db.SysObjectInModules.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.SysObjectInModules.AddRange(
                new SysObjectInModule { ObjectCode = "OBJ_INVOICE", ModuleCode = "MOD_INVOICE", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysObjectInModule { ObjectCode = "OBJ_REPORT", ModuleCode = "MOD_REPORT", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysObjectInModule { ObjectCode = "OBJ_USER", ModuleCode = "MOD_SYS", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysObjectInModule { ObjectCode = "OBJ_DEALER", ModuleCode = "MOD_SYS", LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
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
