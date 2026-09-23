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
                new MstNnt { MST = "0101234567", NNTFullName = "Công ty Demo A", NNTAddress = "Hà Nội", MSTBUPattern = "ALL.0101234567%", NetworkID = "NET-DEMO", DLCode = "DL001", ProvinceCode = "01", DistrictCode = "001", GovTaxID = "CQT-HN", NNTMobile = "0901234567", NNTPhone = "0243123456", PresentBy = "Nguyễn Văn A", ContactName = "Trần Thị B", ContactEmail = "a@demo.vn", RegisterStatus = "ACTIVE" },
                new MstNnt { MST = "0107654321", NNTFullName = "Công ty Demo B", NNTAddress = "TP.HCM", MSTBUPattern = "ALL.0107654321%", NetworkID = "NET-DEMO", DLCode = "DL002", ProvinceCode = "79", DistrictCode = "760", GovTaxID = "CQT-HCM", NNTMobile = "0907654321", NNTPhone = "0283123456", PresentBy = "Lê Văn C", ContactName = "Phạm Thị D", ContactEmail = "b@demo.vn", RegisterStatus = "ACTIVE" },
                new MstNnt { MST = "0109999999", NNTFullName = "Công ty Demo C (ngừng)", NNTAddress = "Đà Nẵng", MSTBUPattern = "ALL.0109999999%", NetworkID = "NET-DEMO", DLCode = "DL003", ProvinceCode = "48", DistrictCode = "490", GovTaxID = "CQT-DN", RegisterStatus = "INACTIVE", FlagActive = false });
            await db.SaveChangesAsync();
        }
        // Danh mục cơ quan thuế (Mst_GovTaxID) + tỉnh (Mst_Province) + huyện (Mst_District) cho danh sách NNT.
        if (!await db.MstGovTaxIds.AnyAsync())
        {
            db.MstGovTaxIds.AddRange(
                new MstGovTaxId { GovTaxID = "CQT-HN", GovTaxName = "Cục Thuế TP Hà Nội" },
                new MstGovTaxId { GovTaxID = "CQT-HCM", GovTaxName = "Cục Thuế TP Hồ Chí Minh" },
                new MstGovTaxId { GovTaxID = "CQT-DN", GovTaxName = "Cục Thuế TP Đà Nẵng" });
            await db.SaveChangesAsync();
        }
        // Danh mục loại giấy tờ (Mst_GovIDType) cho danh sách loại giấy tờ (RptSv_Mst_GovIDType_Get).
        if (!await db.MstGovIdTypes.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.MstGovIdTypes.AddRange(
                new MstGovIdType { GovIDType = "CMND", NetworkID = "NET-DEMO", GovIDTypeName = "Chứng minh nhân dân", Remark = "CMND 9 số cấp trước 2016", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MstGovIdType { GovIDType = "CCCD", NetworkID = "NET-DEMO", GovIDTypeName = "Căn cước công dân", Remark = "CCCD gắn chip 12 số", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MstGovIdType { GovIDType = "HOPCHIEU", NetworkID = "NET-DEMO", GovIDTypeName = "Hộ chiếu", Remark = "Passport", LogLUDTimeUTC = upd, LogLUBy = "nnt_a" },
                new MstGovIdType { GovIDType = "DKKD", NetworkID = "NET-DEMO", GovIDTypeName = "Giấy chứng nhận ĐKKD", Remark = "Giấy phép kinh doanh của tổ chức", LogLUDTimeUTC = upd, LogLUBy = "nnt_b" },
                new MstGovIdType { GovIDType = "GIAYTO_CU", NetworkID = "NET-DEMO", GovIDTypeName = "Giấy tờ cũ (ngừng)", Remark = "Loại không còn dùng", FlagActive = false, LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
            await db.SaveChangesAsync();
        }
        if (!await db.MstProvinces.AnyAsync())
        {
            db.MstProvinces.AddRange(
                new MstProvince { ProvinceCode = "01", ProvinceName = "Hà Nội" },
                new MstProvince { ProvinceCode = "79", ProvinceName = "Hồ Chí Minh" },
                new MstProvince { ProvinceCode = "48", ProvinceName = "Đà Nẵng" });
            await db.SaveChangesAsync();
        }
        if (!await db.MstDistricts.AnyAsync())
        {
            db.MstDistricts.AddRange(
                new MstDistrict { ProvinceCode = "01", DistrictCode = "001", DistrictName = "Quận Ba Đình" },
                new MstDistrict { ProvinceCode = "79", DistrictCode = "760", DistrictName = "Quận 1" },
                new MstDistrict { ProvinceCode = "48", DistrictCode = "490", DistrictName = "Quận Hải Châu" });
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
        // Danh mục đại lý (Mst_Dealer) cho danh sách đại lý (RptSv_Mst_Dealer_Get).
        if (!await db.MstDealers.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.MstDealers.AddRange(
                new MstDealer { DLCode = "DL001", NetworkID = "NET-DEMO", DLCodeParent = "VPBANK", DLBUCode = "IDOCNET.VPBANK.DL001", DLBUPattern = "IDOCNET.VPBANK.DL001%", DLLevel = "1", DLType = "CAP1", ProvinceCode = "01", DLName = "Đại lý Hà Nội", DLAddress = "Quận Ba Đình, Hà Nội", DLPresentBy = "Nguyễn Văn A", DLGovIDNumber = "001199001234", DLEmail = "dl001@demo.vn", DLPhoneNo = "0901234567", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MstDealer { DLCode = "DL002", NetworkID = "NET-DEMO", DLCodeParent = "VPBANK", DLBUCode = "IDOCNET.VPBANK.DL002", DLBUPattern = "IDOCNET.VPBANK.DL002%", DLLevel = "2", DLType = "CAP2", ProvinceCode = "79", DLName = "Đại lý Hồ Chí Minh", DLAddress = "Quận 1, TP.HCM", DLPresentBy = "Lê Văn C", DLGovIDNumber = "079199002345", DLEmail = "dl002@demo.vn", DLPhoneNo = "0907654321", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MstDealer { DLCode = "DL003", NetworkID = "NET-DEMO", DLCodeParent = "VPBANK", DLBUCode = "IDOCNET.VPBANK.DL003", DLBUPattern = "IDOCNET.VPBANK.DL003%", DLLevel = "2", DLType = "CAP2", ProvinceCode = "48", DLName = "Đại lý Đà Nẵng (ngừng)", DLAddress = "Quận Hải Châu, Đà Nẵng", DLPresentBy = "Trần Thị D", DLGovIDNumber = "048199003456", DLEmail = "dl003@demo.vn", DLPhoneNo = "0909999999", FlagActive = false, LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
            await db.SaveChangesAsync();
        }
        // Danh mục phương thức thanh toán (Mst_PaymentMethods) cho danh sách phương thức thanh toán.
        if (!await db.MstPaymentMethods.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.MstPaymentMethods.AddRange(
                new MstPaymentMethod { PaymentMethodCode = "TM", NetworkID = "NET-DEMO", PaymentMethodName = "Tiền mặt", Remark = "Thanh toán bằng tiền mặt", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MstPaymentMethod { PaymentMethodCode = "CK", NetworkID = "NET-DEMO", PaymentMethodName = "Chuyển khoản", Remark = "Thanh toán qua ngân hàng", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MstPaymentMethod { PaymentMethodCode = "TM/CK", NetworkID = "NET-DEMO", PaymentMethodName = "Tiền mặt/Chuyển khoản", Remark = "Kết hợp tiền mặt và chuyển khoản", LogLUDTimeUTC = upd, LogLUBy = "nnt_a" },
                new MstPaymentMethod { PaymentMethodCode = "QT", NetworkID = "NET-DEMO", PaymentMethodName = "Quẹt thẻ", Remark = "Thanh toán bằng thẻ ngân hàng", FlagActive = false, LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
            await db.SaveChangesAsync();
        }
        // Giải pháp hệ thống (Sys_Solution) + mô-đun (Sys_Modules) cho danh sách mô-đun hệ thống.
        if (!await db.SysSolutions.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.SysSolutions.AddRange(
                new SysSolution { SolutionCode = "SOL_HDDT", NetworkID = "NET-DEMO", SolutionName = "Hóa đơn điện tử", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysSolution { SolutionCode = "SOL_QLDL", NetworkID = "NET-DEMO", SolutionName = "Quản lý đại lý", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysSolution { SolutionCode = "SOL_KT", NetworkID = "NET-DEMO", SolutionName = "Kế toán tổng hợp", FlagActive = false, LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
            await db.SaveChangesAsync();
        }
        if (!await db.SysModules.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.SysModules.AddRange(
                new SysModule { ModuleCode = "MOD_INVOICE", NetworkID = "NET-DEMO", SolutionCode = "SOL_HDDT", ModuleName = "Quản lý hóa đơn", Description = "Phát hành & quản lý hóa đơn điện tử", QtyInvoice = 100000, ValCapacity = 50m, LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysModule { ModuleCode = "MOD_REPORT", NetworkID = "NET-DEMO", SolutionCode = "SOL_HDDT", ModuleName = "Báo cáo tổng hợp", Description = "Báo cáo tình hình sử dụng hóa đơn", QtyInvoice = 0, ValCapacity = 10m, LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new SysModule { ModuleCode = "MOD_DEALER", NetworkID = "NET-DEMO", SolutionCode = "SOL_QLDL", ModuleName = "Quản lý đại lý", Description = "Danh mục & chiết khấu đại lý", QtyInvoice = 0, ValCapacity = 20m, LogLUDTimeUTC = upd, LogLUBy = "nnt_a" },
                new SysModule { ModuleCode = "MOD_ACCOUNT", NetworkID = "NET-DEMO", SolutionCode = "SOL_KT", ModuleName = "Kế toán tổng hợp", Description = "Hạch toán & đối soát", QtyInvoice = 0, ValCapacity = 5m, FlagActive = false, LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
            await db.SaveChangesAsync();
        }
        // Danh mục loại hóa đơn (Mst_InvoiceType) cho danh sách loại hóa đơn (RptSv_Mst_InvoiceType_Get).
        if (!await db.MstInvoiceTypes.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.MstInvoiceTypes.AddRange(
                new MstInvoiceType { InvoiceType = "01GTKT", NetworkID = "NET-DEMO", InvoiceTypeName = "Hóa đơn giá trị gia tăng", Remark = "Hóa đơn GTGT thông thường", TTType = "GTGT", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MstInvoiceType { InvoiceType = "02GTTT", NetworkID = "NET-DEMO", InvoiceTypeName = "Hóa đơn bán hàng", Remark = "Hóa đơn bán hàng thông thường", TTType = "BANHANG", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new MstInvoiceType { InvoiceType = "04HDDT", NetworkID = "NET-DEMO", InvoiceTypeName = "Hóa đơn điện tử", Remark = "Hóa đơn điện tử có mã của CQT", TTType = "HDDT", LogLUDTimeUTC = upd, LogLUBy = "nnt_a" },
                new MstInvoiceType { InvoiceType = "07GTKT", NetworkID = "NET-DEMO", InvoiceTypeName = "Hóa đơn GTGT (ngừng)", Remark = "Loại cũ không còn dùng", TTType = "GTGT", FlagActive = false, LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
            await db.SaveChangesAsync();
        }
        // Danh mục loại hình/lĩnh vực/quy mô (iNOS_Mst_BizType/BizField/BizSize) + tổ chức iNOS (MstSv_Inos_Org)
        // cho danh sách tổ chức iNOS (RptSv_MstSv_Inos_Org_Get).
        if (!await db.InosMstBizTypes.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.InosMstBizTypes.AddRange(
                new InosMstBizType { BizType = "BT01", NetworkID = "NET-DEMO", BizTypeName = "Công ty TNHH", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new InosMstBizType { BizType = "BT02", NetworkID = "NET-DEMO", BizTypeName = "Công ty cổ phần", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new InosMstBizType { BizType = "BT03", NetworkID = "NET-DEMO", BizTypeName = "Hộ kinh doanh", FlagActive = false, LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
            await db.SaveChangesAsync();
        }
        if (!await db.InosMstBizFields.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.InosMstBizFields.AddRange(
                new InosMstBizField { BizFieldCode = "BF01", NetworkID = "NET-DEMO", BizFieldName = "Thương mại", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new InosMstBizField { BizFieldCode = "BF02", NetworkID = "NET-DEMO", BizFieldName = "Sản xuất", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new InosMstBizField { BizFieldCode = "BF03", NetworkID = "NET-DEMO", BizFieldName = "Dịch vụ", LogLUDTimeUTC = upd, LogLUBy = "nnt_a" });
            await db.SaveChangesAsync();
        }
        if (!await db.InosMstBizSizes.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.InosMstBizSizes.AddRange(
                new InosMstBizSize { BizSizeCode = "BS01", NetworkID = "NET-DEMO", BizSizeName = "Nhỏ", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new InosMstBizSize { BizSizeCode = "BS02", NetworkID = "NET-DEMO", BizSizeName = "Vừa", LogLUDTimeUTC = upd, LogLUBy = "admin" },
                new InosMstBizSize { BizSizeCode = "BS03", NetworkID = "NET-DEMO", BizSizeName = "Lớn", LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
            await db.SaveChangesAsync();
        }
        if (!await db.MstSvInosOrgs.AnyAsync())
        {
            var upd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(1);
            db.MstSvInosOrgs.AddRange(
                new MstSvInosOrg { MST = "0101234567", InosId = 501, ParentId = null, Name = "Công ty Demo A", BizType = "BT01", BizField = "BF01", OrgSize = "BS02", ContactName = "Nguyễn Văn A", Email = "a@demo.vn", PhoneNo = "0901234567", Description = "Tổ chức gốc Demo A", Enable = true, CurrentUserRole = "OWNER", LogLUDTimeUTC = upd, LogLUBy = "admin", OrderId = 1001 },
                new MstSvInosOrg { MST = "0101234567", InosId = 502, ParentId = 501, Name = "Chi nhánh Demo A - HCM", BizType = "BT01", BizField = "BF03", OrgSize = "BS01", ContactName = "Trần Thị B", Email = "hcm@demo.vn", PhoneNo = "0901234568", Description = "Chi nhánh phía Nam", Enable = true, CurrentUserRole = "MEMBER", LogLUDTimeUTC = upd, LogLUBy = "nnt_a", OrderId = 1003 },
                new MstSvInosOrg { MST = "0107654321", InosId = 503, ParentId = null, Name = "Công ty Demo B", BizType = "BT02", BizField = "BF02", OrgSize = "BS03", ContactName = "Lê Văn C", Email = "b@demo.vn", PhoneNo = "0907654321", Description = "Tổ chức Demo B", Enable = true, CurrentUserRole = "OWNER", LogLUDTimeUTC = upd, LogLUBy = "nnt_b", OrderId = 1002 },
                new MstSvInosOrg { MST = "0109999999", InosId = 504, ParentId = null, Name = "Hộ kinh doanh Demo C (ngừng)", BizType = "BT03", BizField = "BF01", OrgSize = "BS01", ContactName = "Phạm Thị D", Email = "c@demo.vn", PhoneNo = "0909999999", Description = "Đã ngừng hoạt động", Enable = false, CurrentUserRole = "MEMBER", FlagActive = false, LogLUDTimeUTC = upd, LogLUBy = "nnt_b" });
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
