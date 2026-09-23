namespace MiniGate.Models;

public class Org
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
public interface IOrgOwned { Guid OrgId { get; set; } }

/// <summary>Tuyến định tuyến: /gw/{Prefix}/... → {UpstreamBaseUrl}/...</summary>
public class GwRoute : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = "";
    public string Prefix { get; set; } = "";            // segment đầu sau /gw (vd "pim")
    public string UpstreamBaseUrl { get; set; } = "";   // vd https://minipim.onrender.com
    public bool RequireAuth { get; set; }               // cần X-Api-Key hợp lệ
    public bool IsActive { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>Client (ứng dụng gọi qua cổng) — có API key + hạn mức.</summary>
public class ApiClient : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int RateLimitPerMin { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Nhật ký mỗi request đi qua cổng.</summary>
public class RequestLog : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int? RouteId { get; set; }
    public string RouteName { get; set; } = "";
    public string ClientName { get; set; } = "anonymous";
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string UpstreamUrl { get; set; } = "";
    public int StatusCode { get; set; }
    public long LatencyMs { get; set; }
    public DateTime At { get; set; } = DateTime.Now;
}

/// <summary>Mẫu hóa đơn (TempInvoice) — dải số phát hành cho một ký hiệu/mẫu số.</summary>
public class InvoiceTemplate : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string TInvoiceCode { get; set; } = "";   // mã mẫu
    public string TInvoiceName { get; set; } = "";   // tên mẫu
    public string InvoiceType { get; set; } = "";    // loại hóa đơn
    public string FormNo { get; set; } = "";         // mẫu số
    public string Sign { get; set; } = "";           // ký hiệu
    public string MST { get; set; } = "";            // MST người nộp thuế
    public long StartInvoiceNo { get; set; }          // từ số
    public long EndInvoiceNo { get; set; }            // đến số
    public DateTime EffDateStart { get; set; }        // ngày hiệu lực
    public bool FlagActive { get; set; } = true;
}

/// <summary>Hóa đơn phát hành (Invoice) — dùng để tổng hợp tình hình sử dụng.</summary>
public class Invoice : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string TInvoiceCode { get; set; } = "";    // trỏ tới mẫu
    public long? InvoiceNo { get; set; }              // số hóa đơn
    public DateTime InvoiceDate { get; set; }         // ngày lập
    public string InvoiceStatus { get; set; } = "";   // ISSUED / DELETED / CANCELED / PENDING
    public string MST { get; set; } = "";             // MST người nộp thuế
    public decimal AmountAfterVAT { get; set; }       // tổng tiền sau VAT (Σ UnitPrice*Qty*(1+VAT))
    public DateTime CreatedAt { get; set; } = DateTime.Now; // thời điểm tạo (CreateDTimeUTC)
}

/// <summary>Hạn mức phát hành hóa đơn theo MST (Invoice_license) — dùng cho bảng điều khiển.</summary>
public class InvoiceLicense : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string MST { get; set; } = "";             // MST người nộp thuế
    public string NetworkId { get; set; } = "";       // mạng/đại lý
    public long TotalQty { get; set; }                // tổng số được cấp
    public long TotalQtyIssued { get; set; }          // đã phát hành
    public long TotalQtyUsed { get; set; }            // đã sử dụng
    public long TotalQtyCancel { get; set; }          // đã hủy
    public bool FlagActive { get; set; } = true;
}

/// <summary>
/// Người nộp thuế (Mst_NNT) — danh mục MST dùng cho phân quyền xem dữ liệu.
/// MSTBUPattern là mẫu LIKE (vd "ALL.0313304214%") xác định các MST mà một user được phép xem.
/// </summary>
public class MstNnt : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string MST { get; set; } = "";             // MST người nộp thuế
    public string NNTFullName { get; set; } = "";     // tên NNT
    public string NNTAddress { get; set; } = "";      // địa chỉ
    public string MSTBUPattern { get; set; } = "";    // mẫu LIKE phân quyền xem (ViewAbility)
    public string NetworkID { get; set; } = "";       // mạng/đại lý
    public string DLCode { get; set; } = "";          // mã đại lý
    public string ProvinceCode { get; set; } = "";    // mã tỉnh
    public string DistrictCode { get; set; } = "";    // mã huyện
    public string GovTaxID { get; set; } = "";        // cơ quan thuế quản lý
    public string NNTMobile { get; set; } = "";       // ĐT di động
    public string NNTPhone { get; set; } = "";        // ĐT cố định
    public string PresentBy { get; set; } = "";       // người đại diện
    public string ContactName { get; set; } = "";     // tên người liên lạc
    public string ContactEmail { get; set; } = "";    // email người liên hệ
    public string RegisterStatus { get; set; } = "";  // trạng thái đăng ký
    public bool FlagActive { get; set; } = true;
}

/// <summary>
/// Loại giấy tờ (Mst_GovIDType) — danh mục loại giấy tờ tùy thân/tổ chức
/// (CMND/CCCD/hộ chiếu/ĐKKD...) dùng khi đăng ký người nộp thuế.
/// Dùng cho danh sách loại giấy tờ (RptSv_Mst_GovIDType_Get).
/// </summary>
public class MstGovIdType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string GovIDType { get; set; } = "";        // mã loại giấy tờ
    public string NetworkID { get; set; } = "";        // mạng/đại lý
    public string GovIDTypeName { get; set; } = "";    // tên loại giấy tờ
    public string Remark { get; set; } = "";           // ghi chú
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }         // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";          // người cập nhật cuối
}

/// <summary>
/// Cơ quan thuế (Mst_GovTaxID) — danh mục cơ quan thuế quản lý NNT.
/// Dùng cho danh sách người nộp thuế (RptSv_Mst_NNT_Get).
/// </summary>
public class MstGovTaxId : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string GovTaxID { get; set; } = "";        // mã cơ quan thuế
    public string GovTaxName { get; set; } = "";      // tên cơ quan thuế
    public bool FlagActive { get; set; } = true;
}

/// <summary>
/// Tỉnh/thành (Mst_Province) — danh mục tỉnh dùng cho địa chỉ NNT.
/// Dùng cho danh sách người nộp thuế (RptSv_Mst_NNT_Get).
/// </summary>
public class MstProvince : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string ProvinceCode { get; set; } = "";    // mã tỉnh
    public string ProvinceName { get; set; } = "";    // tên tỉnh
    public bool FlagActive { get; set; } = true;
}

/// <summary>
/// Quận/huyện (Mst_District) — danh mục huyện dùng cho địa chỉ NNT.
/// Dùng cho danh sách người nộp thuế (RptSv_Mst_NNT_Get).
/// </summary>
public class MstDistrict : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string ProvinceCode { get; set; } = "";    // mã tỉnh
    public string DistrictCode { get; set; } = "";    // mã huyện
    public string DistrictName { get; set; } = "";    // tên huyện
    public bool FlagActive { get; set; } = true;
}

/// <summary>
/// Người dùng hệ thống (Sys_User) — dùng cho phân quyền theo MST (Mst_NNT_ViewAbility).
/// FlagSysAdmin = true → xem toàn bộ MST; ngược lại chỉ xem MST khớp MSTBUPattern của NNT gắn với user.
/// </summary>
public class SysUser : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string UserCode { get; set; } = "";        // mã đăng nhập
    public string UserName { get; set; } = "";        // tên hiển thị
    public string UserNick { get; set; } = "";        // biệt danh (UserNick)
    public string BankCode { get; set; } = "";        // mã ngân hàng (BankCode)
    public string MST { get; set; } = "";             // MST của user (gắn tới Mst_NNT)
    public bool FlagSysAdmin { get; set; }            // quản trị hệ thống → xem tất cả
    public bool FlagDLAdmin { get; set; }             // quản trị đại lý (FlagDLAdmin)
    public bool FlagActive { get; set; } = true;
}

/// <summary>Nhóm quyền (Sys_Group) — dùng để gom user theo nhóm chức năng.</summary>
public class SysGroup : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string GroupCode { get; set; } = "";       // mã nhóm
    public string GroupName { get; set; } = "";       // tên nhóm
    public string MST { get; set; } = "";             // MST gắn với nhóm (nếu có)
    public bool FlagActive { get; set; } = true;
}

/// <summary>Thành viên nhóm (RptSv_Sys_UserInGroup) — liên kết user ↔ nhóm.</summary>
public class SysUserInGroup : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string UserCode { get; set; } = "";        // mã user
    public string GroupCode { get; set; } = "";       // mã nhóm
}

/// <summary>
/// Nhóm mẫu hóa đơn (Invoice_TempGroup) — gom các mẫu hóa đơn theo một bộ trường tùy biến.
/// Dùng cho danh sách nhóm mẫu (RptSv_Invoice_TempGroup_Get).
/// </summary>
public class InvoiceTempGroup : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string InvoiceTGroupCode { get; set; } = "";  // mã nhóm mẫu
    public string MST { get; set; } = "";                 // MST người nộp thuế
    public string InvoiceTGroupName { get; set; } = "";   // tên nhóm mẫu
    public string InvoiceTGroupBody { get; set; } = "";   // nội dung/thân nhóm mẫu
    public string FilePathThumbnail { get; set; } = "";   // ảnh thu nhỏ
    public string Spec_Prd_Type { get; set; } = "";       // loại sản phẩm đặc thù
    public bool FlagActive { get; set; } = true;
}

/// <summary>
/// Trường tùy biến của nhóm mẫu (Invoice_TempGroupField) — liên kết nhóm mẫu ↔ trường DB.
/// DBFieldName trỏ tới Invoice_CustomField / Invoice_DtlCustomField.
/// </summary>
public class InvoiceTempGroupField : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string InvoiceTGroupCode { get; set; } = "";  // mã nhóm mẫu
    public string DBFieldName { get; set; } = "";         // tên trường DB
    public string NetworkId { get; set; } = "";           // mạng/đại lý
    public string TCFType { get; set; } = "";             // loại trường (TCFType)
    public bool FlagActive { get; set; } = true;
}

/// <summary>
/// Đơn hàng license (LicOrder) — đơn mua gói/bản quyền phần mềm hóa đơn.
/// Dùng cho báo cáo hoa hồng đơn hàng license (RptSv_InosLicOrder_Commission).
/// </summary>
public class LicOrder : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public long OrderId { get; set; }                 // mã đơn hàng (OrderId)
    public string OrgName { get; set; } = "";        // tên tổ chức mua (InosLicOrderOrgName)
    public string DiscountCode { get; set; } = "";   // mã chiết khấu (InosLicOrderDiscountCode)
    public decimal TotalCost { get; set; }            // giá vốn (InosLicOrderTotalCost)
    public decimal Price { get; set; }                // giá bán (InosLicOrderPrice)
    public string PaymentCode { get; set; } = "";    // mã thanh toán (InosLicOrderPaymentCode)
    public string PaymentStatusDesc { get; set; } = ""; // mô tả trạng thái thanh toán
    public string OrderStatus { get; set; } = "";    // trạng thái đơn (InosLicOrderStatus)
    public string PaymentStatus { get; set; } = "";  // trạng thái thanh toán (InosLicPaymentStatuses)
    public DateTime CreateDTime { get; set; }         // thời điểm tạo đơn
    public DateTime? ApproveDTime { get; set; }       // thời điểm duyệt đơn
    public string CreateUserId { get; set; } = "";   // người tạo
    public string Remark { get; set; } = "";         // ghi chú
    public long InosOrgId { get; set; }               // id tổ chức (InosOrgId)
    public long InosNetworkId { get; set; }           // id mạng/đại lý (InosNetworkId)
    public string MST { get; set; } = "";            // MST người nộp thuế (join Mst_NNT)
    public string DLCode { get; set; } = "";         // mã đại lý (join Mst_NNT)
}

/// <summary>
/// Hoa hồng đơn hàng license (RptSv_InosLicOrder_Commission) — thông tin hoa hồng theo vai trò
/// (người trình bày/telesale/tư vấn/triển khai) gắn với một đơn hàng license.
/// </summary>
public class LicOrderCommission : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public long OrderId { get; set; }                 // trỏ tới LicOrder.OrderId
    public string CommissionStatus { get; set; } = ""; // trạng thái hoa hồng (PENDING/APPROVED/PAID)
    public string Remark { get; set; } = "";         // ghi chú hoa hồng
    public string Presenter1 { get; set; } = "";     // người trình bày 1
    public string Presenter2 { get; set; } = "";     // người trình bày 2
    public string Telesale { get; set; } = "";       // telesale
    public string Consultants { get; set; } = "";    // tư vấn
    public string Implementer { get; set; } = "";    // người triển khai
    public decimal CommissionPresenter1 { get; set; }
    public decimal CommissionPresenter2 { get; set; }
    public decimal CommissionTelesale { get; set; }
    public decimal CommissionConsultants { get; set; }
    public decimal CommissionImplementer { get; set; }
}

/// <summary>
/// Cấu hình chiết khấu đại lý (Map_DealerDiscount) — ánh xạ mã đại lý (DLCode) ↔ mã chiết khấu (DiscountCode).
/// Dùng cho danh sách cấu hình chiết khấu đại lý (RptSv_Map_DealerDiscount_Get).
/// </summary>
public class MapDealerDiscount : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string DLCode { get; set; } = "";          // mã đại lý
    public string DiscountCode { get; set; } = "";    // mã chiết khấu
    public bool FlagActive { get; set; } = true;       // đang áp dụng
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
}

/// <summary>
/// Đối tượng/chức năng hệ thống (RptSv_Sys_Object) — danh mục các đối tượng phân quyền
/// (màn hình/nghiệp vụ) mà nhóm có thể được cấp quyền truy cập.
/// Dùng cho danh sách quyền truy cập của nhóm (RptSv_Sys_Access_Get).
/// </summary>
public class SysObject : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string ObjectCode { get; set; } = "";      // mã đối tượng
    public string NetworkID { get; set; } = "";       // mạng/đại lý
    public string ObjectName { get; set; } = "";      // tên đối tượng
    public string ServiceCode { get; set; } = "";     // mã dịch vụ
    public string ObjectType { get; set; } = "";      // loại đối tượng
    public string FlagExecModal { get; set; } = "";   // chạy dạng modal
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
}

/// <summary>
/// Quyền truy cập của nhóm theo đối tượng (RptSv_Sys_Access) — liên kết nhóm (GroupCode) ↔ đối tượng (ObjectCode).
/// Dùng cho danh sách quyền truy cập của nhóm (RptSv_Sys_Access_Get).
/// </summary>
public class SysAccess : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string GroupCode { get; set; } = "";       // mã nhóm
    public string ObjectCode { get; set; } = "";      // mã đối tượng
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
}

/// <summary>
/// Đối tượng trong mô-đun (Sys_ObjectInModules) — liên kết đối tượng/chức năng (ObjectCode)
/// với mô-đun (ModuleCode). Dùng cho danh sách đối tượng trong mô-đun (RptSv_Sys_ObjectInModules_Get).
/// </summary>
public class SysObjectInModule : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string ObjectCode { get; set; } = "";      // mã đối tượng/chức năng
    public string ModuleCode { get; set; } = "";      // mã mô-đun
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
}

/// <summary>
/// Thuế suất GTGT (Mst_VATRate) — danh mục thuế suất dùng khi lập hóa đơn.
/// Dùng cho danh sách thuế suất (RptSv_Mst_VATRate_Get).
/// </summary>
public class MstVatRate : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string VATRateCode { get; set; } = "";     // mã thuế suất
    public string NetworkID { get; set; } = "";       // mạng/đại lý
    public decimal VATRate { get; set; }               // giá trị thuế suất (%)
    public string VATDesc { get; set; } = "";         // mô tả thuế suất
    public string Remark { get; set; } = "";          // ghi chú
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
}

/// <summary>
/// Đại lý (Mst_Dealer) — danh mục đại lý phân cấp (cấp 1/cấp 2) theo mạng/đại lý.
/// Dùng cho danh sách đại lý (RptSv_Mst_Dealer_Get).
/// </summary>
public class MstDealer : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string DLCode { get; set; } = "";          // mã đại lý
    public string NetworkID { get; set; } = "";       // mạng/đại lý
    public string DLCodeParent { get; set; } = "";    // mã đại lý cấp trên
    public string DLBUCode { get; set; } = "";        // mã đơn vị nghiệp vụ (DLBUCode)
    public string DLBUPattern { get; set; } = "";     // mẫu LIKE đơn vị nghiệp vụ (DLBUPattern)
    public string DLLevel { get; set; } = "";         // cấp đại lý (DLLevel)
    public string DLType { get; set; } = "";          // loại đại lý (DLType)
    public string ProvinceCode { get; set; } = "";    // mã tỉnh
    public string DLName { get; set; } = "";          // tên đại lý
    public string DLAddress { get; set; } = "";       // địa chỉ
    public string DLPresentBy { get; set; } = "";     // người đại diện
    public string DLGovIDNumber { get; set; } = "";   // số giấy tờ (DLGovIDNumber)
    public string DLEmail { get; set; } = "";         // email
    public string DLPhoneNo { get; set; } = "";       // điện thoại
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
}

/// <summary>
/// Phương thức thanh toán (Mst_PaymentMethods) — danh mục hình thức thanh toán dùng khi lập hóa đơn.
/// Dùng cho danh sách phương thức thanh toán (RptSv_Mst_PaymentMethods_Get).
/// </summary>
public class MstPaymentMethod : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string PaymentMethodCode { get; set; } = "";  // mã phương thức thanh toán
    public string NetworkID { get; set; } = "";          // mạng/đại lý
    public string PaymentMethodName { get; set; } = "";  // tên phương thức thanh toán
    public string Remark { get; set; } = "";             // ghi chú
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }           // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";            // người cập nhật cuối
}

/// <summary>
/// Loại hóa đơn (Mst_InvoiceType) — danh mục loại hóa đơn dùng khi lập/phát hành hóa đơn.
/// Dùng cho danh sách loại hóa đơn (RptSv_Mst_InvoiceType_Get).
/// </summary>
public class MstInvoiceType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string InvoiceType { get; set; } = "";       // mã loại hóa đơn
    public string NetworkID { get; set; } = "";         // mạng/đại lý
    public string InvoiceTypeName { get; set; } = "";   // tên loại hóa đơn
    public string Remark { get; set; } = "";            // ghi chú
    public string TTType { get; set; } = "";            // loại TT (TTType)
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }           // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";           // người cập nhật cuối
}

/// <summary>
/// Giải pháp hệ thống (Sys_Solution) — nhóm các mô-đun theo một giải pháp nghiệp vụ.
/// Dùng cho danh sách mô-đun hệ thống (RptSv_Sys_Modules_Get).
/// </summary>
public class SysSolution : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string SolutionCode { get; set; } = "";    // mã giải pháp
    public string NetworkID { get; set; } = "";       // mạng/đại lý
    public string SolutionName { get; set; } = "";    // tên giải pháp
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
}

/// <summary>
/// Mô-đun hệ thống (Sys_Modules) — đơn vị chức năng thuộc một giải pháp (Sys_Solution),
/// kèm hạn mức số hóa đơn (QtyInvoice) và dung lượng (ValCapacity).
/// Dùng cho danh sách mô-đun hệ thống (RptSv_Sys_Modules_Get).
/// </summary>
public class SysModule : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string ModuleCode { get; set; } = "";      // mã mô-đun
    public string NetworkID { get; set; } = "";       // mạng/đại lý
    public string SolutionCode { get; set; } = "";    // mã giải pháp (join Sys_Solution)
    public string ModuleName { get; set; } = "";      // tên mô-đun
    public string Description { get; set; } = "";     // mô tả
    public long QtyInvoice { get; set; }               // hạn mức số hóa đơn
    public decimal ValCapacity { get; set; }           // dung lượng (ValCapacity)
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
}

/// <summary>
/// Loại hình kinh doanh (iNOS_Mst_BizType) — danh mục loại hình (BizType) dùng cho tổ chức iNOS.
/// Dùng cho danh sách tổ chức iNOS (RptSv_MstSv_Inos_Org_Get).
/// </summary>
public class InosMstBizType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string BizType { get; set; } = "";        // mã loại hình kinh doanh
    public string NetworkID { get; set; } = "";      // mạng/đại lý
    public string BizTypeName { get; set; } = "";    // tên loại hình kinh doanh
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
}

/// <summary>
/// Lĩnh vực kinh doanh (iNOS_Mst_BizField) — danh mục lĩnh vực (BizField) dùng cho tổ chức iNOS.
/// Dùng cho danh sách tổ chức iNOS (RptSv_MstSv_Inos_Org_Get).
/// </summary>
public class InosMstBizField : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string BizFieldCode { get; set; } = "";   // mã lĩnh vực kinh doanh
    public string NetworkID { get; set; } = "";      // mạng/đại lý
    public string BizFieldName { get; set; } = "";   // tên lĩnh vực kinh doanh
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
}

/// <summary>
/// Quy mô tổ chức (iNOS_Mst_BizSize) — danh mục quy mô (BizSize) dùng cho tổ chức iNOS.
/// Dùng cho danh sách tổ chức iNOS (RptSv_MstSv_Inos_Org_Get).
/// </summary>
public class InosMstBizSize : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string BizSizeCode { get; set; } = "";    // mã quy mô tổ chức
    public string NetworkID { get; set; } = "";      // mạng/đại lý
    public string BizSizeName { get; set; } = "";    // tên quy mô tổ chức
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
}

/// <summary>
/// Tổ chức iNOS (MstSv_Inos_Org) — tổ chức/đơn vị trong hệ sinh thái iNOS, gắn với MST,
/// loại hình (BizType), lĩnh vực (BizField) và quy mô (OrgSize).
/// Dùng cho danh sách tổ chức iNOS (RptSv_MstSv_Inos_Org_Get).
/// </summary>
public class MstSvInosOrg : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string MST { get; set; } = "";            // MST người nộp thuế
    public long InosId { get; set; }                  // id tổ chức (Id)
    public long? ParentId { get; set; }               // id tổ chức cấp trên (ParentId)
    public string Name { get; set; } = "";           // tên tổ chức
    public string BizType { get; set; } = "";        // loại hình kinh doanh (join iNOS_Mst_BizType)
    public string BizField { get; set; } = "";       // lĩnh vực kinh doanh (join iNOS_Mst_BizField)
    public string OrgSize { get; set; } = "";        // quy mô tổ chức (join iNOS_Mst_BizSize)
    public string ContactName { get; set; } = "";    // người liên hệ
    public string Email { get; set; } = "";          // email
    public string PhoneNo { get; set; } = "";        // điện thoại
    public string Description { get; set; } = "";    // mô tả
    public bool Enable { get; set; } = true;          // đang kích hoạt
    public string CurrentUserRole { get; set; } = ""; // vai trò người dùng hiện tại
    public bool FlagActive { get; set; } = true;
    public DateTime LogLUDTimeUTC { get; set; }        // thời điểm cập nhật cuối
    public string LogLUBy { get; set; } = "";         // người cập nhật cuối
    public long? OrderId { get; set; }                // mã đơn hàng (OrderId)
}
