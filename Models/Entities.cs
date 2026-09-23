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
