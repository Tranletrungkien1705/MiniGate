// ─────────────────────────────────────────────────────────────────────────────
// Port 1:1 từ dự án nguồn 2019.9.MobileGate (Dev/V10) — lớp MODEL / PHONG BÌ JSON.
// MobileGate là CỔNG API cho mobile (gateway), KHÔNG có màn hình UI cho các
// endpoint dưới đây, nên phần port là tham số + mã lỗi + hình dạng phong bì.
//
// Nguồn (đường dẫn tương đối trong D:\idocNet\2019.9.MobileGate\Dev\V10):
//   idN.MobileGate.Common/Models/JsonResult.cs:11-112   -> MgFieldError/MgErrorData/MgJsonResultEntry
//   idN.MobileGate.Common/Models/WARQBase.cs:9-50       -> MgWARQBase
//   idN.MobileGate.Common/Models/WARTBase.cs:9-14       -> MgWARTBase
//   idN.MobileGate.Common/Models/RQ_Common.cs:9-12      -> MgRQ_Common
//   idN.MobileGate.Common/Models/RT_Common.cs:9-11      -> MgRT_Common
//   idN.MobileGate.Common/Models/MySummaryTable.cs      -> MgMySummaryTable
//   idN.MobileGate.Common/Models/c_K_DT_Sys.cs:9-16     -> Mg_c_K_DT_Sys
//   idN.MobileGate.Common/Models/c_K_DT_SysInfo.cs      -> Mg_c_K_DT_SysInfo
//   idN.MobileGate.Common/Models/c_K_DT_SysError.cs     -> Mg_c_K_DT_SysError
//   idN.MobileGate.Common/Models/c_K_DT_SysWarning.cs   -> Mg_c_K_DT_SysWarning
//   idN.MobileGate.Common/Models/SysUser.cs             -> MgSysUser
//   idN.MobileGate.Common/Models/SysUserRT.cs:8-17      -> MgSysUserRT
//   idN.MobileGate.Common/Models/RQ_MstSv_Sys_User.cs   -> MgRQ_MstSv_Sys_User
//   idN.MobileGate.Common/Models/RT_MstSv_Sys_User.cs   -> MgRT_MstSv_Sys_User
//   idN.MobileGate.Common/Models/MstSv_Sys_User.cs      -> MgMstSv_Sys_User
//   idN.MobileGate.Common/Models/MstSv_Mst_Network.cs   -> MstSvNetwork (entity, xem Entities.cs)
//
// Tiền tố "Mg" thêm vào để tránh trùng tên với model sẵn có của MiniGate
// (đã có Org/ApiClient/RequestLog). Tên THÀNH VIÊN giữ NGUYÊN 1:1 vì client
// mobile bind theo tên thành viên trong JSON.
// ─────────────────────────────────────────────────────────────────────────────

namespace MiniGate.Models;

/// <summary>Nguồn: JsonResult.cs:11-15.</summary>
public class MgFieldError
{
    public object? FieldName { get; set; }
    public object? ErrorMessage { get; set; }
}

/// <summary>Nguồn: JsonResult.cs:16-22.</summary>
public class MgErrorData
{
    public string? ErrorCode { get; set; }
    public object? Message { get; set; }
    public List<MgFieldError>? FieldError { get; set; }
    public object? Trace { get; set; }
}

/// <summary>
/// Phong bì JSON chuẩn của cổng. Nguồn: JsonResult.cs:23-112.
/// Cố ý bỏ ctor nhận ModelStateDictionary + AddModelState (nguồn JsonResult.cs:33-63):
/// đó là API của ASP.NET MVC5, .NET 8 có ModelState khác hẳn nên không port 1:1 được.
/// </summary>
public class MgJsonResultEntry
{
    public MgJsonResultEntry()
    {
        // Nguồn: JsonResult.cs:25-31 — mặc định Success=true, mảng RỖNG (không null), ErrorData rỗng.
        Success = true;
        Messages = Array.Empty<object>();
        FieldErrors = Array.Empty<MgFieldError>();
        ErrorData = new MgErrorData();
    }

    /// <summary>Nguồn: JsonResult.cs:39-43.</summary>
    public MgJsonResultEntry SetFailed() { Success = false; return this; }

    /// <summary>Nguồn: JsonResult.cs:45-50.</summary>
    public MgJsonResultEntry SetSuccess() { Success = true; return this; }

    public bool Success { get; set; }
    public int ErrorCode { get; set; }
    public object[] Messages { get; set; }
    public object? Model { get; set; }
    public object? Data { get; set; }
    public object? Detail { get; set; }
    public object? ErrorData { get; set; }
    public object? RedirectUrl { get; set; }

    // Nguồn: JsonResult.cs:76-77 — mặc định true.
    private bool _redirectToOpener = true;
    public bool RedirectToOpener { get => _redirectToOpener; set => _redirectToOpener = value; }

    public MgFieldError[] FieldErrors { get; set; }

    /// <summary>Nguồn: JsonResult.cs:79-93 — LƯU Ý: thêm field-error thì Success bị đặt false.</summary>
    public MgJsonResultEntry AddFieldError(object? fieldName, object? message)
    {
        Success = false;
        FieldErrors = FieldErrors.Concat(new[] { new MgFieldError { FieldName = fieldName, ErrorMessage = message } }).ToArray();
        return this;
    }

    /// <summary>Nguồn: JsonResult.cs:94-99 — thân TRÙNG AddFieldError (nguồn viết trùng, giữ 1:1).</summary>
    public MgJsonResultEntry AddErrorData(string fieldName, string message)
    {
        Success = false;
        FieldErrors = FieldErrors.Concat(new[] { new MgFieldError { FieldName = fieldName, ErrorMessage = message } }).ToArray();
        return this;
    }

    /// <summary>Nguồn: JsonResult.cs:100-104.</summary>
    public MgJsonResultEntry AddMessage(string message)
    {
        Messages = Messages.Concat(new object[] { message }).ToArray();
        return this;
    }

    /// <summary>Nguồn: JsonResult.cs:105-111.</summary>
    public MgJsonResultEntry AddException(Exception e) { Success = false; return AddMessage(e.Message); }
}

/// <summary>
/// Nguồn: mbiz.core.Data.Services.ServiceResult&lt;T&gt; — DLL ngoài (05.Refs/Libs),
/// KHÔNG có mã nguồn trong repo; hình dạng suy từ cách dùng ở ApiControllerBase.cs:32-62
/// (đúng 3 thành viên Data / Success / Exception).
/// Cố ý đơn giản hoá: nguồn nhét thẳng đối tượng Exception (Newtonsoft tự serialize);
/// ở đây gói thành { Message, StackTraceString } vì System.Text.Json không serialize Exception.
/// </summary>
public class MgServiceResult<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public object? Exception { get; set; }
}

/// <summary>Nguồn: WARQBase.cs:9-50 — giữ nguyên tên thành viên, kể cả pageIndex/pageSize viết thường.</summary>
public class MgWARQBase
{
    public string? Tid { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? TokenID { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? UtcOffset { get; set; }
    public string? GwUserCode { get; set; }
    public string? GwPassword { get; set; }
    public string? WAUserCode { get; set; }
    public string? WAUserPassword { get; set; }
    public string? WAUserCodeActual { get; set; }
    public string? FlagIsDelete { get; set; }
    public string? FlagAppr { get; set; }
    public string? FuncType { get; set; }
    public string? Ft_RecordStart { get; set; }
    public string? Ft_RecordCount { get; set; }
    public string? Ft_WhereClause { get; set; }
    public string? Ft_Cols_Upd { get; set; }
    // Nguồn: WARQBase.cs:47-49 — chú thích nguồn ghi "Yêu cầu của Tùng"; tên lệch quy ước, giữ nguyên.
    public string? pageIndex { get; set; }
    public string? pageSize { get; set; }
}

/// <summary>Nguồn: WARTBase.cs:9-14.</summary>
public class MgWARTBase
{
    public Mg_c_K_DT_Sys? c_K_DT_Sys { get; set; }
    public MgMySummaryTable? MySummaryTable { get; set; }
}

/// <summary>Nguồn: MySummaryTable.cs.</summary>
public class MgMySummaryTable { public object? MyCount { get; set; } }

/// <summary>Nguồn: c_K_DT_Sys.cs:9-16.</summary>
public class Mg_c_K_DT_Sys
{
    public List<Mg_c_K_DT_SysInfo>? Lst_c_K_DT_SysInfo { get; set; }
    public List<Mg_c_K_DT_SysError>? Lst_c_K_DT_SysError { get; set; }
    public List<Mg_c_K_DT_SysWarning>? Lst_c_K_DT_SysWarning { get; set; }
}

/// <summary>Nguồn: c_K_DT_SysInfo.cs — 7 thành viên, đúng thứ tự.</summary>
public class Mg_c_K_DT_SysInfo
{
    public string? Tid { get; set; }
    public string? DigitalSignature { get; set; }
    public string? ErrorCode { get; set; }
    public string? FlagWarning { get; set; }
    public string? Remark { get; set; }
    public string? FlagCompress { get; set; }
    public string? FlagEncrypt { get; set; }
}

/// <summary>Nguồn: c_K_DT_SysError.cs.</summary>
public class Mg_c_K_DT_SysError { public string? PCode { get; set; } public string? PVal { get; set; } }

/// <summary>Nguồn: c_K_DT_SysWarning.cs.</summary>
public class Mg_c_K_DT_SysWarning { public string? PCode { get; set; } public string? PVal { get; set; } }

/// <summary>Nguồn: RQ_Common.cs:9-12 — chỉ thêm AppVersion (chú thích nguồn: 20201202, Mobile truyền kèm Header).</summary>
public class MgRQ_Common : MgWARQBase { public string? AppVersion { get; set; } }

/// <summary>Nguồn: RT_Common.cs:9-11 — rỗng, chỉ kế thừa WARTBase.</summary>
public class MgRT_Common : MgWARTBase { }

/// <summary>
/// Nguồn: SysUser.cs — 10 thành viên. LƯU Ý nguồn để Password ở model TRẢ VỀ và
/// AccountController.cs:17-21 echo lại mật khẩu cho client (giữ 1:1, xem báo cáo mục 5).
/// </summary>
public class MgSysUser
{
    public string? Code { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Password { get; set; }
    public bool SysAdmin { get; set; }
    public bool Enable { get; set; }
    public bool Lockout { get; set; }
    public DateTime LockoutDate { get; set; }
    public string? Language { get; set; }
}

/// <summary>Nguồn: SysUserRT.cs:8-17 — 4 danh sách, hậu tố "Lst" (KHÁC tiền tố "Lst_" của WARTBase).</summary>
public class MgSysUserRT
{
    public List<MgSysUser>? SysUserLst { get; set; }
    public List<Mg_c_K_DT_SysInfo>? c_K_DT_SysInfoLst { get; set; }
    public List<Mg_c_K_DT_SysError>? c_K_DT_SysErrorLst { get; set; }
    public List<Mg_c_K_DT_SysWarning>? c_K_DT_SysWarningLst { get; set; }
}

/// <summary>Nguồn: MstSv_Sys_User.cs — mọi thành viên kiểu object (nguồn để object thật).</summary>
public class MgMstSv_Sys_User
{
    public object? UserCode { get; set; }
    public object? UserName { get; set; }
    public object? UserPassword { get; set; }
    public object? UserPasswordNew { get; set; }
    public object? UserEmail { get; set; }
    public object? CreateDTimeUTC { get; set; }
    public object? EffDTimeUTC { get; set; }
    public object? FlagActive { get; set; }
    public object? LogLUDTimeUTC { get; set; }
    public object? LogLUBy { get; set; }
}

/// <summary>Nguồn: RQ_MstSv_Sys_User.cs.</summary>
public class MgRQ_MstSv_Sys_User : MgWARQBase
{
    public string? Rt_Cols_MstSv_Sys_User { get; set; }
    public MgMstSv_Sys_User? MstSv_Sys_User { get; set; }
}

/// <summary>Nguồn: RT_MstSv_Sys_User.cs.</summary>
public class MgRT_MstSv_Sys_User : MgWARTBase
{
    public List<MgMstSv_Sys_User>? Lst_MstSv_Sys_User { get; set; }
}

/// <summary>
/// Mã lỗi port NGUYÊN VĂN từ idN.MobileGate.Constants — chuỗi phải khớp tuyệt đối
/// vì client mobile so sánh theo chuỗi.
/// </summary>
public static class MgErr
{
    // Error.DMS.Chain.cs:19 — chú thích nguồn: "Lỗi Hệ thống, khi truy nhập trái phép vào Cổng giao dịch."
    public const string CmSys_GatewayAuthenticateFailed = "ErridN.MobileGate.CmSys_GatewayAuthenticateFailed";
    // Error.DMS.Chain.cs:42
    public const string WAS_Cm_GetDTime = "ErridN.MobileGate.WAS_Cm_GetDTime";
    // Error.idN.TVANMasterServer.cs:58 — VN: "Lỗi đăng nhập người dùng" (Error.idN.TVAN.VN.cs:205)
    public const string WAS_MstSv_Sys_User_Login = "ErridNTVAN.WAS_MstSv_Sys_User_Login";
    // Error.idN.TVANMasterServer.cs:30
    public const string MstSv_Sys_User_Login = "ErridNTVAN.MstSv_Sys_User_Login";
    // Error.idN.TVANMasterServer.cs:75 — VN: "Lỗi Không tìm thấy network trong hệ thống" (VN.cs:209)
    public const string MstSv_Mst_Network_CheckDB_NetworkNotFound = "ErridNTVAN.MstSv_Mst_Network_CheckDB_NetworkNotFound";
    // Error.idN.TVANMasterServer.cs:77 — VN: "Lỗi trạng thái network không phù hợp" (VN.cs:211)
    public const string MstSv_Mst_Network_CheckDB_FlagActiveNotMatched = "ErridNTVAN.MstSv_Mst_Network_CheckDB_FlagActiveNotMatched";

    /// <summary>
    /// Nguồn: Const.Main.BE.cs:57-65 — Flag.Yes == Flag.Active == "1"; Flag.No == Flag.Inactive == "0".
    /// (Đã kiểm thật: vì Yes == Active nên nhánh "NetworkNotFound" ở MasterServer.cs:1408 CÓ chạy,
    ///  không phải mã chết như thoạt nhìn.)
    /// </summary>
    public const string FlagActive = "1";
    public const string FlagInactive = "0";
}
