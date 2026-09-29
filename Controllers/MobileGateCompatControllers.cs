// ─────────────────────────────────────────────────────────────────────────────
// Port 1:1 ENDPOINT của dự án nguồn 2019.9.MobileGate (Dev/V10) sang MiniGate (.NET 8).
//
// MobileGate KHÔNG CÓ UI cho các endpoint này — nó là cổng API cho mobile, mỗi "màn"
// thực chất là 1 action POST. Vì vậy bản port đối chiếu TOÀN BỘ tham số + mã lỗi,
// không có bảng cột / trang Razor nào.
//
// Đường dẫn giữ NGUYÊN 1:1: nguồn dùng route MVC mặc định {controller}/{action},
// MiniGate cũng đăng ký "{controller=Home}/{action=Index}/{id?}" (Program.cs:115),
// nên /Account/Login, /Common/WA_Common_GetDTime, /MasterServer/WA_MstSv_Sys_User_Login
// trùng khít đường dẫn nguồn.
//
// Ba endpoint được port trong file này:
//   1. POST /Account/Login (+ MyLogin, MyLogin1, WebAPILogin, WebAPILogin1)
//      nguồn: idN.MobileGate.Biz.Web/Controllers/AccountController.cs:10-186
//   2. POST /Common/WA_Common_GetDTime
//      nguồn: idN.MobileGate.Biz.Web/Controllers/CommonController.cs:22-181
//            + idN.MobileGate.Biz/Common.cs:1131-1186 (WAS_Cm_GetDTime)
//            + idN.MobileGate.Biz/Common.cs:755-766   (Cm_GetDTime)
//   3. POST /MasterServer/WA_MstSv_Sys_User_Login
//      nguồn: idN.MobileGate.Biz.Web/Controllers/MasterServerController.cs:19-90
//            + idN.MobileGate.Biz/MasterServer.cs:759-850 (WAS_MstSv_Sys_User_Login)
//            + idN.MobileGate.Biz/MasterServer.cs:431-570 (MstSv_Sys_User_Login)
//            + idN.MobileGate.Biz/MasterServer.cs:1382-1446 (MstSv_Mst_Network_CheckDB)
// ─────────────────────────────────────────────────────────────────────────────

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniGate.Data;
using MiniGate.Models;

namespace MiniGate.Controllers;

/// <summary>
/// Port 1:1 lớp nền idN.MobileGate.Biz.Web/Controllers/ApiControllerBase.cs:29-345.
/// Giữ: Success/Error, GetNextTId, CheckAuthorize (3 quá tải còn sống), ProcessException.
/// Cố ý bỏ: ctor nạp _biz.LoadInitAPIUrl_OutSide_New20200115 (ApiControllerBase.cs:288-316)
///          và RenderRazorViewToString (ApiControllerBase.cs:332-343) — thuộc tầng biz/MVC5
///          không có ở MiniGate. Xem báo cáo mục 5.
/// </summary>
public abstract class MgApiControllerBase(AppDbContext db, IConfiguration cfg) : Controller
{
    protected readonly AppDbContext _db = db;
    protected readonly IConfiguration _cfg = cfg;

    // Nguồn: ApiControllerBase.cs:31 — biến đếm THEO INSTANCE, không static.
    private int _inext = 0;

    /// <summary>
    /// Nguồn MVC5 + Newtonsoft serialize theo ĐÚNG tên thành viên (PascalCase: "Success", "Data",
    /// "c_K_DT_Sys"...). .NET 8 mặc định đổi sang camelCase, sẽ làm client mobile mất field.
    /// Tuỳ chọn này tắt việc đổi tên để khớp 1:1 dây dẫn, và KHÔNG đụng cấu hình JSON toàn app
    /// (SPA admin của MiniGate vẫn dùng camelCase như cũ).
    /// </summary>
    protected static readonly System.Text.Json.JsonSerializerOptions MgJson = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Trả JSON theo đúng tên thành viên như nguồn.</summary>
    protected IActionResult MgJsonResult(object? data) => Json(data, MgJson);

    /// <summary>Nguồn: ApiControllerBase.cs:32-39.</summary>
    protected MgServiceResult<T> Success<T>(T data) => new() { Data = data, Success = true };

    /// <summary>Nguồn: ApiControllerBase.cs:41-49 — Data = default(T), Exception = ex.</summary>
    protected MgServiceResult<T> Error<T>(Exception ex) => new()
    {
        Data = default,
        Success = false,
        Exception = ExcInfo(ex)
    };

    /// <summary>
    /// Nguồn: ApiControllerBase.cs:51-62. LƯU Ý 1:1: quá tải này ĐẶT Exception = null
    /// (nguồn ghi rõ `Exception = null`, dòng 60) — tham số ex bị bỏ, CỐ Ý giữ nguyên
    /// vì client dựa vào việc lỗi nằm trong Data.c_K_DT_Sys chứ không nằm ở Exception.
    /// </summary>
    protected MgServiceResult<T> Error<T>(Exception ex, T data) => new()
    {
        Data = data,
        Success = false,
        Exception = null
    };

    /// <summary>Gói Exception thành hình dạng serialize được (xem chú thích MgServiceResult).</summary>
    protected static object ExcInfo(Exception ex) => new { Message = ex.Message, StackTraceString = ex.StackTrace };

    /// <summary>
    /// Nguồn: ApiControllerBase.cs:326-331 — Tid = "yyyyMMdd.HHmmss.ffffff.{số thứ tự}".
    /// Số thứ tự tăng dần theo instance controller (mỗi request là 1 instance).
    /// </summary>
    protected string GetNextTId()
    {
        var strDateTimeNow = DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff").Trim();
        return string.Format("{0}.{1}", strDateTimeNow, _inext++);
    }

    /// <summary>
    /// Lấy token từ header Authorization dạng "Bearer xxx".
    /// Nguồn: CommonController.cs:45-50 — CHỈ nhận khi có tiền tố "Bearer " (có dấu cách),
    /// không thì strAuthorization = null.
    /// </summary>
    protected string? ReadBearer()
    {
        var strVal = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(strVal) && strVal.StartsWith("Bearer ")) return strVal.Substring(7);
        return null;
    }

    protected string? AccessToken { get; set; }
    protected long MyOrgId { get; set; }

    /// <summary>
    /// Nguồn: ApiControllerBase.cs:179-210 — quá tải 3 tham số, LÀ quá tải mà
    /// CommonController.cs:82 thực sự gọi.
    /// Thứ tự 1:1:
    ///   1. strOrgId khác rỗng -> MyOrgId = long.Parse (nguồn KHÔNG try/catch, chuỗi sai ném luôn).
    ///   2. strAuthorization rỗng -> false.
    ///   3. token == AppSettings["MbGAccessToken"] -> strUserCode = AppSettings["WAUserCodeBG"], true
    ///      (đường tắt cho hệ thống nội bộ, KHÔNG gọi iNOS).
    ///   4. ngược lại -> gọi iNOS AccountService.GetCurrentUser(); có user -> strUserCode = user.Email.
    /// CỐ Ý ĐƠN GIẢN HOÁ ở bước 4: MiniGate không có iNOS (inos.common). Thay bằng tra
    /// ApiClient.ApiKey của chính MiniGate (bảng Clients) — cùng vai trò "đổi token ra danh tính",
    /// strUserCode = ApiClient.Name. Xem báo cáo mục 5.
    /// </summary>
    protected async Task<(bool ok, string? strUserCode)> CheckAuthorizeAsync(string? strAuthorization, string? strOrgId)
    {
        if (!string.IsNullOrEmpty(strOrgId)) MyOrgId = long.Parse(strOrgId);

        string strMbGAccessToken = _cfg["MbGAccessToken"] ?? "";

        if (string.IsNullOrEmpty(strAuthorization)) return (false, null);

        // Nguồn: ApiControllerBase.cs:189 — StringEqual = so sánh CÓ phân biệt hoa/thường.
        if (!string.Equals(strAuthorization, strMbGAccessToken, StringComparison.Ordinal))
        {
            AccessToken = strAuthorization;
            // Thay iNOS GetCurrentUser() bằng tra client của cổng (xem chú thích trên).
            var client = await _db.Clients.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.ApiKey == strAuthorization && c.IsActive);
            if (client != null) return (true, client.Name);
            return (false, null);
        }

        // Nguồn: ApiControllerBase.cs:202-206 — nhánh đường tắt MbGAccessToken.
        return (true, _cfg["WAUserCodeBG"]);
    }

    /// <summary>
    /// Nguồn: idN.MobileGate.Utils/Utils.cs:3040-3057 CConnectionManager.CheckGatewayAuthentication.
    /// KHÁC BIỆT QUAN TRỌNG 1:1: GwUserCode so sánh BỎ QUA hoa/thường (StringEqualIgnoreCase),
    /// GwPassword so sánh CÓ phân biệt hoa/thường (StringEqual). Sai -> ném lỗi
    /// ErridN.MobileGate.CmSys_GatewayAuthenticateFailed.
    /// Nguồn đọc cặp Biz_GwUserCode / Biz_GwPassword từ _biz._cf.nvcParams (web.config AppSettings).
    /// </summary>
    protected void CheckGatewayAuthentication(string? strGwUserCode, string? strGwPassword)
    {
        var cfgUser = _cfg["Biz_GwUserCode"] ?? "";
        var cfgPass = _cfg["Biz_GwPassword"] ?? "";
        if (!string.Equals(strGwUserCode ?? "", cfgUser, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(strGwPassword ?? "", cfgPass, StringComparison.Ordinal))
        {
            throw new MgServiceException(MgErr.CmSys_GatewayAuthenticateFailed);
        }
    }

    /// <summary>
    /// Nguồn: ApiControllerBase.cs:239-275 — dựng JsonResultEntry từ Exception.
    /// ServiceException -> ErrorData { ErrorCode, Message, Trace }; khác -> ErrorCode = "".
    /// </summary>
    protected MgJsonResultEntry ProcessException(Exception ex)
    {
        var ret = new MgJsonResultEntry();
        if (ex is MgServiceException sx)
            ret.ErrorData = new { ErrorCode = sx.ErrorCode, Message = sx.ErrorMessage, Trace = sx.StackTrace };
        else
            ret.ErrorData = new { ErrorCode = "", Message = ex.Message, Trace = ex.StackTrace };
        ret.Success = false;
        return ret;
    }

    /// <summary>
    /// Dựng c_K_DT_Sys từ mã lỗi + các cặp tham số lỗi.
    /// Nguồn: idN.MobileGate.Utils/CmUtilsV30.cs:32-63 CMyDataList.CProcessMyDS đổ 3 DataTable
    /// c_K_DT_SysInfo / c_K_DT_SysError / c_K_DT_SysWarning của MyDataSet thành 3 danh sách.
    /// MyDataSet nằm trong CommonUtils.dll (05.Refs/Libs/Common/CommonUtils.dll — KHÔNG có mã
    /// nguồn trong repo), nên bố cục dưới đây SUY từ cách dùng: Tid + ErrorCode + Remark nằm ở
    /// c_K_DT_SysInfo (CProcessMyDS + CommonController.cs:162 GetErrorCode + Common.cs:761 SetRemark),
    /// các cặp PCode/PVal nằm ở c_K_DT_SysError (CommonController.cs:157-160).
    /// </summary>
    protected static Mg_c_K_DT_Sys BuildSys(string? tid, string? errorCode = null, string? remark = null,
        params (string PCode, string PVal)[] errs) => new()
        {
            Lst_c_K_DT_SysInfo = new List<Mg_c_K_DT_SysInfo>
            {
                new() { Tid = tid, ErrorCode = errorCode, Remark = remark }
            },
            Lst_c_K_DT_SysError = errs.Select(e => new Mg_c_K_DT_SysError { PCode = e.PCode, PVal = e.PVal }).ToList(),
            Lst_c_K_DT_SysWarning = new List<Mg_c_K_DT_SysWarning>()
        };
}

/// <summary>
/// Port của mbiz.core.Exceptions.ServiceException / CmUtils.CMyException.Raise
/// (DLL ngoài, không có mã nguồn). Chỉ cần 2 thứ mà nguồn thực sự đọc:
/// ErrorCode (ApiControllerBase.cs:250) và ErrorMessage (ApiControllerBase.cs:258).
/// Nguồn CMyException.Raise đặt Message = strErrorCode (CmUtilsV30.cs:96-105), giữ đúng thế.
/// </summary>
public class MgServiceException(string errorCode, params (string PCode, string PVal)[] paramsCouple)
    : Exception(errorCode)
{
    public string ErrorCode { get; } = errorCode;
    public string ErrorMessage { get; } = errorCode;
    public (string PCode, string PVal)[] ParamsCouple { get; } = paramsCouple;
}

// ═════════════════════════════════════════════════════════════════════════════
// ENDPOINT 1 — /Account/*  (port 1:1 AccountController.cs:10-186)
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Port 1:1 idN.MobileGate.Biz.Web/Controllers/AccountController.cs:10-186.
/// CẢNH BÁO 1:1 — cả 5 action ở NGUỒN đều là STUB: không truy vấn DB, không sinh token,
/// không kiểm mật khẩu. Bản port giữ đúng hành vi stub đó (kể cả dữ liệu cứng "cso3"/"123456")
/// để khớp 1:1; KHÔNG được coi đây là cơ chế đăng nhập thật. Xem báo cáo mục 5.
/// Nguồn KHÔNG gọi CheckAuthorize / CheckGatewayAuthentication ở controller này (đúng như vậy).
/// </summary>
public class AccountController(AppDbContext db, IConfiguration cfg) : MgApiControllerBase(db, cfg)
{
    /// <summary>
    /// Nguồn: AccountController.cs:12-27. [AcceptVerbs("POST")] -> [HttpPost].
    /// Tham số: username, password (bind từ form/query, nguồn là MVC5 nên nhận cả hai).
    /// Hành vi 1:1: TRẢ LẠI chính username/password vừa nhận, Success = true. Không xác thực gì.
    /// </summary>
    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        try
        {
            var sysUser = new MgSysUser { Code = username, Password = password };
            return MgJsonResult(Success(sysUser));
        }
        catch (Exception ex) { return MgJsonResult(Error<MgSysUser>(ex)); }
    }

    /// <summary>
    /// Nguồn: AccountController.cs:29-50.
    /// Hành vi 1:1: nguồn gói objsysuser vào lstSysUserCur rồi KHÔNG DÙNG (dòng 35 là mã chết),
    /// luôn trả về đúng 1 phần tử cứng cso3/123456/CSO3. Giữ nguyên, kể cả biến chết.
    /// </summary>
    [HttpPost]
    public IActionResult MyLogin([FromBody] MgSysUser objsysuser)
    {
        try
        {
            // Nguồn: AccountController.cs:35 — biến này nguồn tạo ra rồi bỏ không dùng.
            var lstSysUserCur = new List<MgSysUser> { objsysuser };
            var lstSysUser = new List<MgSysUser>
            {
                new() { Code = "cso3", Password = "123456", FullName = "CSO3" },
            };
            return MgJsonResult(Success(lstSysUser));
        }
        catch (Exception ex) { return MgJsonResult(Error<List<MgSysUser>>(ex)); }
    }

    /// <summary>
    /// Nguồn: AccountController.cs:52-73 — nhận CHUỖI JSON (không phải object) rồi
    /// deserialize thành List&lt;SysUser&gt;, nhưng cũng KHÔNG dùng kết quả; vẫn trả cso3 cứng.
    /// 1:1 giữ cả việc deserialize (vì JSON sai định dạng thì nguồn ném và rơi vào catch).
    /// </summary>
    [HttpPost]
    public IActionResult MyLogin1(string objsysuer)
    {
        try
        {
            var lstSysUserInput = System.Text.Json.JsonSerializer.Deserialize<List<MgSysUser>>(objsysuer);
            var lstSysUser = new List<MgSysUser>
            {
                new() { Code = "cso3", Password = "123456", FullName = "CSO3" },
            };
            return MgJsonResult(Success(lstSysUser));
        }
        catch (Exception ex) { return MgJsonResult(Error<List<MgSysUser>>(ex)); }
    }

    /// <summary>
    /// Nguồn: AccountController.cs:75-118.
    /// Hành vi 1:1: echo Code/Password/FullName vào SysUserLst, và LUÔN thêm 1 dòng
    /// c_K_DT_SysInfo có ErrorCode = "Invalid Error" (nguồn dòng 107) DÙ Success = true.
    /// Đây là điểm dễ hiểu sai nhất của endpoint này — giữ đúng nguyên văn chuỗi.
    /// c_K_DT_SysErrorLst và c_K_DT_SysWarningLst nguồn để NULL (không khởi tạo).
    /// </summary>
    [HttpPost]
    public IActionResult WebAPILogin([FromBody] MgSysUser objsysuser)
    {
        try
        {
            var objSysUserRT = new MgSysUserRT
            {
                SysUserLst = new List<MgSysUser>(),
                c_K_DT_SysInfoLst = new List<Mg_c_K_DT_SysInfo>()
            };

            var objSysUser = new MgSysUser
            {
                Code = objsysuser?.Code,
                Password = objsysuser?.Password,
                FullName = objsysuser?.FullName
            };
            objSysUserRT.SysUserLst.Add(objSysUser);

            // Nguồn: AccountController.cs:105-110 — chuỗi "Invalid Error" viết cứng.
            objSysUserRT.c_K_DT_SysInfoLst.Add(new Mg_c_K_DT_SysInfo { ErrorCode = "Invalid Error" });

            return MgJsonResult(Success(objSysUserRT));
        }
        catch (Exception ex) { return MgJsonResult(Error<MgSysUserRT>(ex)); }
    }

    /// <summary>
    /// Nguồn: AccountController.cs:120-155 — nhận SysUserRT, bỏ hết (dòng 130-147 bị comment),
    /// chỉ trả về SysUserLst RỖNG (khác null) và 3 danh sách còn lại để null.
    /// </summary>
    [HttpPost]
    public IActionResult WebAPILogin1([FromBody] MgSysUserRT objsysuser)
    {
        try
        {
            var objSysUserRT = new MgSysUserRT { SysUserLst = new List<MgSysUser>() };
            return MgJsonResult(Success(objSysUserRT));
        }
        catch (Exception ex) { return MgJsonResult(Error<MgSysUserRT>(ex)); }
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ENDPOINT 2 — POST /Common/WA_Common_GetDTime
//   nguồn: CommonController.cs:22-181 + Biz/Common.cs:1131-1186 + Biz/Common.cs:755-766
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Port 1:1 idN.MobileGate.Biz.Web/Controllers/CommonController.cs:20-182.
/// Đây là endpoint "mẫu nhà" của cổng: mọi endpoint nghiệp vụ khác của MobileGate
/// đều lặp lại đúng trình tự 6 bước này, nên port nó = port toàn bộ đường ống cổng.
/// </summary>
public class CommonController(AppDbContext db, IConfiguration cfg) : MgApiControllerBase(db, cfg)
{
    /// <summary>
    /// POST /Common/WA_Common_GetDTime — lấy giờ hệ thống của cổng.
    /// Nguồn: CommonController.cs:22-181.
    ///
    /// THAM SỐ (1:1):
    ///   - Thân: objRQ_Common (RQ_Common) — dùng WAUserCode, pageIndex, pageSize.
    ///   - Thân: strRQ_Map_IDInBox (string JSON) — nếu KHÁC NULL thì deserialize thành RQ_Common
    ///     và GHI ĐÈ toàn bộ objRQ_Common (CommonController.cs:54-61). Tên tham số là
    ///     "Map_IDInBox" vì action được copy từ endpoint eTem — nguồn để nguyên, giữ 1:1.
    ///   - Header "Authorization: Bearer {token}"  (CommonController.cs:46-50)
    ///   - Header "networkId"                      (CommonController.cs:72)
    ///   - Header "orgId"                          (CommonController.cs:73)
    ///   - Header "AppVer"                         (CommonController.cs:74, thêm 20201202)
    ///
    /// MÃ LỖI (1:1):
    ///   - Authorize sai  -> { Success:false, Message:"Authorize is false", Detail:"Authorize is false" }
    ///     (CommonController.cs:85-91 — KHÔNG phải phong bì JsonResultEntry, là object riêng 3 field)
    ///   - Gateway sai    -> ErridN.MobileGate.CmSys_GatewayAuthenticateFailed
    ///   - Lỗi biz mặc định -> ErridN.MobileGate.WAS_Cm_GetDTime (Error.DMS.Chain.cs:42)
    ///
    /// TRẢ VỀ (1:1): JsonResultEntry.Data = RT_Common, trong đó giờ server nằm ở
    /// c_K_DT_Sys.Lst_c_K_DT_SysInfo[0].Remark, định dạng "yyyy-MM-dd HH:mm:ss.ffffff"
    /// theo giờ UTC (Biz/Common.cs:761: SetRemark(DateTime.UtcNow...)) — KHÔNG phải giờ máy.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> WA_Common_GetDTime([FromForm] MgRQ_Common? objRQ_Common, [FromForm] string? strRQ_Map_IDInBox)
    {
        #region // Temp: (nguồn CommonController.cs:28-42)
        objRQ_Common ??= new MgRQ_Common();
        MgRT_Common? objRT_Common = null;
        DateTime dtimeSys = DateTime.Now;
        string strControllerName = "WA_Common_GetDTime";
        string strFunctionName = "WA_Common_GetDTime";
        var alParamsCoupleError = new List<(string, string)>
        {
            ("strControllerName", strControllerName),
            ("strFunctionName", strFunctionName),
            ("dtimeSys", dtimeSys.ToString("yyyy-MM-dd HH:mm:ss")),
        };
        #endregion

        #region // Refine and Check Token: (nguồn CommonController.cs:44-102)
        string? strAuthorization = ReadBearer();

        // Nguồn: CommonController.cs:53 — giữ lại WAUserCode TRƯỚC khi có thể bị ghi đè,
        // rồi gán lại ở dòng 69. Thứ tự này quan trọng: JSON ghi đè KHÔNG thắng được WAUserCode.
        var usercode = objRQ_Common.WAUserCode;
        if (strRQ_Map_IDInBox != null)
        {
            var objRQMap_IDInBox = System.Text.Json.JsonSerializer.Deserialize<MgRQ_Common>(strRQ_Map_IDInBox);
            if (objRQMap_IDInBox != null) objRQ_Common = objRQMap_IDInBox;
        }

        // Nguồn: CommonController.cs:63-69.
        objRQ_Common.Ft_RecordStart = objRQ_Common.pageIndex;
        objRQ_Common.Ft_RecordCount = objRQ_Common.pageSize;
        objRQ_Common.GwUserCode = _cfg["GwUserCode"] ?? "";
        objRQ_Common.GwPassword = _cfg["GwPassword"] ?? "";
        objRQ_Common.AccessToken = strAuthorization;
        objRQ_Common.Tid = GetNextTId();
        objRQ_Common.WAUserCode = usercode;

        string? strNetworID = Request.Headers["networkId"].FirstOrDefault();
        string? strOrgID = Request.Headers["orgId"].FirstOrDefault();
        string? strAppVersion = Request.Headers["AppVer"].FirstOrDefault();   // nguồn dòng 74, thêm 20201202
        var resultEntry = new MgJsonResultEntry { Success = true };

        #region // CheckAuthorize: (nguồn CommonController.cs:78-100)
        try
        {
            var (checkAuthorize, strWAUserCode) = await CheckAuthorizeAsync(strAuthorization, strOrgID);
            if (!checkAuthorize)
            {
                // Nguồn: CommonController.cs:85-91 — trả object 3 field, KHÔNG dùng phong bì chuẩn.
                return MgJsonResult(new { Success = false, Message = "Authorize is false", Detail = "Authorize is false" });
            }
            objRQ_Common.WAUserCode = strWAUserCode;
        }
        catch (Exception ex)
        {
            // Nguồn: CommonController.cs:95-99 — ShowError.ShowErrInos (Utils.cs:3266-3300).
            return MgJsonResult(ShowErrInos(ex, resultEntry, objRQ_Common.Tid, alParamsCoupleError));
        }
        #endregion
        #endregion

        try
        {
            // Nguồn: CommonController.cs:106-113.
            CheckGatewayAuthentication(objRQ_Common.GwUserCode, objRQ_Common.GwPassword);

            #region // Process: (nguồn CommonController.cs:115-139)
            objRQ_Common.NetworkID = strNetworID;
            objRQ_Common.OrgID = strOrgID;
            objRQ_Common.Tid = GetNextTId();          // nguồn gọi GetNextTId LẦN THỨ HAI (dòng 119)
            objRQ_Common.AppVersion = strAppVersion;

            // Biz WAS_Cm_GetDTime (Common.cs:1131-1186) -> Cm_GetDTime (Common.cs:755-766):
            // chỉ đặt Remark = DateTime.UtcNow "yyyy-MM-dd HH:mm:ss.ffffff", không đọc DB.
            objRT_Common = new MgRT_Common();
            objRT_Common.c_K_DT_Sys = BuildSys(
                objRQ_Common.Tid,
                errorCode: null,
                remark: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
            #endregion

            // Return Good: (nguồn CommonController.cs:141-147, LargeJsonResult MaxJsonLength=int.MaxValue)
            resultEntry.Data = objRT_Common;
            return MgJsonResult(resultEntry);
        }
        catch (Exception exc)
        {
            #region // Catch of try: (nguồn CommonController.cs:149-179)
            resultEntry.SetFailed();
            var errdata = new MgErrorData { FieldError = new List<MgFieldError>() };
            errdata.FieldError.Add(new MgFieldError { FieldName = "Exc", ErrorMessage = exc.ToString() });

            var errCode = exc is MgServiceException sx ? sx.ErrorCode : MgErr.WAS_Cm_GetDTime;
            foreach (var (pcode, pval) in alParamsCoupleError)
                errdata.FieldError.Add(new MgFieldError { FieldName = pcode, ErrorMessage = pval });

            errdata.ErrorCode = errCode;
            // Nguồn: CommonController.cs:166 — lấy PHẦN TỬ CUỐI của Lst_c_K_DT_SysError rồi
            // cắt ở "<br/>" đầu tiên làm Message.
            errdata.Message = alParamsCoupleError.Count > 0
                ? alParamsCoupleError[^1].Item2.Split(new[] { "<br/>" }, StringSplitOptions.None)[0]
                : errCode;
            errdata.Trace = exc.ToString();
            resultEntry.ErrorData = errdata;
            return MgJsonResult(resultEntry);
            #endregion
        }
    }

    /// <summary>
    /// Port 1:1 idN.MobileGate.Utils/Utils.cs:3266-3300 ShowError.ShowErrInos.
    /// LƯU Ý 1:1: nguồn CHỈ xử lý khi ex là ServiceException; loại khác thì trả nguyên
    /// resultEntry ban đầu (Success vẫn TRUE) — lỗi bị nuốt câm. Giữ đúng hành vi đó.
    /// </summary>
    private static MgJsonResultEntry ShowErrInos(Exception ex, MgJsonResultEntry resultEntry,
        string? strTid, List<(string, string)> alParamsCoupleError)
    {
        if (ex is MgServiceException cex)
        {
            resultEntry.SetFailed();
            var errdata = new MgErrorData { FieldError = new List<MgFieldError>() };
            errdata.FieldError.Add(new MgFieldError { FieldName = "Exc", ErrorMessage = ex.ToString() });
            foreach (var (pcode, pval) in alParamsCoupleError)
                errdata.FieldError.Add(new MgFieldError { FieldName = pcode, ErrorMessage = pval });
            errdata.ErrorCode = cex.ErrorCode;
            errdata.Message = alParamsCoupleError.Count > 0
                ? alParamsCoupleError[^1].Item2.Split(new[] { "<br/>" }, StringSplitOptions.None)[0]
                : cex.ErrorCode;
            errdata.Trace = ex.ToString();
            resultEntry.ErrorData = errdata;
        }
        return resultEntry;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ENDPOINT 3 — POST /MasterServer/WA_MstSv_Sys_User_Login
//   nguồn: MasterServerController.cs:19-90
//        + Biz/MasterServer.cs:759-850  (WAS_MstSv_Sys_User_Login)
//        + Biz/MasterServer.cs:431-570  (MstSv_Sys_User_Login)
//        + Biz/MasterServer.cs:1382-1446 (MstSv_Mst_Network_CheckDB)
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Port 1:1 idN.MobileGate.Biz.Web/Controllers/MasterServerController.cs:17-90.
/// Đây là endpoint "phân giải tuyến" của cổng: mobile gửi NetworkID, cổng trả về URL
/// upstream (WSUrlAddr) để app gọi tiếp — tương ứng GwRoute.UpstreamBaseUrl của MiniGate.
/// </summary>
public class MasterServerController(AppDbContext db, IConfiguration cfg) : MgApiControllerBase(db, cfg)
{
    /// <summary>
    /// POST /MasterServer/WA_MstSv_Sys_User_Login
    /// Nguồn: MasterServerController.cs:19-90.
    ///
    /// THAM SỐ (1:1, đều nằm trong objRQ_MstSv_Sys_User : WARQBase):
    ///   Tid, GwUserCode, GwPassword, WAUserCode, WAUserPassword, NetworkID, OrgID, TokenID.
    ///   (Rt_Cols_MstSv_Sys_User và MstSv_Sys_User có trong model nhưng luồng Login KHÔNG đọc.)
    ///
    /// MÃ LỖI (1:1):
    ///   - Gateway sai            -> ErridN.MobileGate.CmSys_GatewayAuthenticateFailed
    ///   - NetworkID không có     -> ErridNTVAN.MstSv_Mst_Network_CheckDB_NetworkNotFound
    ///   - FlagActive không khớp  -> ErridNTVAN.MstSv_Mst_Network_CheckDB_FlagActiveNotMatched
    ///   - mặc định của WAS       -> ErridNTVAN.WAS_MstSv_Sys_User_Login
    ///   LƯU Ý: nguồn KHÔNG dùng strErrorCodeDefault từ hằng ở controller mà viết chuỗi
    ///   "WA_MstSv_Sys_User_Login" (MasterServerController.cs:28) — giữ đúng chuỗi trần đó.
    ///
    /// TRẢ VỀ (1:1): ServiceResult&lt;RT_MstSv_Sys_User&gt;, URL upstream nằm ở
    /// Data.c_K_DT_Sys.Lst_c_K_DT_SysInfo[0].Remark (MasterServer.cs:556 SetRemark(WSUrlAddr)).
    /// Lst_MstSv_Sys_User để NULL — nguồn KHÔNG gán (không trả thông tin user nào).
    ///
    /// CẢNH BÁO NGHIỆP VỤ 1:1: endpoint tên là "Login" nhưng KHÔNG kiểm mật khẩu —
    /// khối kiểm UserPassword ở MasterServer.cs:517-525 bị comment, và hai biến
    /// strUserCode/strUserPassword dựng ở MasterServer.cs:502-505 rồi truyền vào
    /// MstSv_Sys_User_Login cũng chỉ được gán lại (dòng 501-502) mà KHÔNG dùng.
    /// Kết quả: ai biết GwUserCode/GwPassword + NetworkID hợp lệ là lấy được URL.
    /// Bản port giữ nguyên để khớp 1:1, KHÔNG tự thêm kiểm mật khẩu. Xem báo cáo mục 5.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> WA_MstSv_Sys_User_Login([FromBody] MgRQ_MstSv_Sys_User? objRQ_MstSv_Sys_User)
    {
        #region // Temp: (nguồn MasterServerController.cs:22-37)
        objRQ_MstSv_Sys_User ??= new MgRQ_MstSv_Sys_User();
        MgRT_MstSv_Sys_User? objRT_MstSv_Sys_User = null;
        DateTime dtimeSys = DateTime.Now;
        string strControllerName = "WA_MstSv_Sys_User_Login";
        // Nguồn: MasterServerController.cs:28 — chuỗi TRẦN, không dùng hằng TError.
        string strErrorCodeDefault = "WA_MstSv_Sys_User_Login";
        var alParamsCoupleError = new List<(string PCode, string PVal)>
        {
            ("strControllerName", strControllerName),
            ("dtimeSys", dtimeSys.ToString("yyyy-MM-dd HH:mm:ss")),
        };
        #endregion

        try
        {
            // Nguồn: MasterServerController.cs:41-48.
            CheckGatewayAuthentication(objRQ_MstSv_Sys_User.GwUserCode, objRQ_MstSv_Sys_User.GwPassword);

            #region // Process: biz WAS_MstSv_Sys_User_Login (MasterServer.cs:759-850)
            // Nguồn: MasterServer.cs:773-780 — hàm WAS bổ sung 2 cặp tham số lỗi.
            alParamsCoupleError.Add(("strFunctionName", "WAS_MstSv_Sys_User_Login"));
            alParamsCoupleError.Add(("dtimeSys", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));

            // Nguồn: MasterServer.cs:789-798 — TokenID trùng Biz_TokenId_idocNet thì gán
            // strUserCode="DUNGND"/strUserPassword="1". HAI BIẾN NÀY LÀ MÃ CHẾT: chúng được
            // truyền xuống MstSv_Sys_User_Login (dòng 815-816) nhưng ở đó chỉ gán lại vào
            // biến cục bộ (dòng 501-502) rồi không ai đọc. Giữ 1:1 kèm ghi chú.
            string? strTokenID = objRQ_MstSv_Sys_User.TokenID?.Trim();
            // Tắt CS0219: hai biến dưới đây CHỦ ĐÍCH không dùng — đó chính là mã chết của
            // nguồn (MasterServer.cs:791-798 gán, dòng 501-502 nhận rồi bỏ). Cảnh báo này là
            // BẰNG CHỨNG port đúng 1:1, không phải lỗi.
#pragma warning disable CS0219
            string? strUserCode = null;
            string? strUserPassword = null;
#pragma warning restore CS0219
            if (string.Equals(strTokenID ?? "", _cfg["Biz_TokenId_idocNet"] ?? "", StringComparison.Ordinal))
            {
                strUserCode = "DUNGND";
                strUserPassword = "1";
            }

            // Nguồn: MasterServer.cs:465-478 — hàm MstSv_Sys_User_Login bổ sung 5 cặp tham số lỗi.
            alParamsCoupleError.Add(("strFunctionName", "MstSv_Sys_User_Login"));
            alParamsCoupleError.Add(("strWAUserCode", objRQ_MstSv_Sys_User.WAUserCode ?? ""));
            alParamsCoupleError.Add(("strWAUserPassword", objRQ_MstSv_Sys_User.WAUserPassword ?? ""));
            alParamsCoupleError.Add(("strNetworkID", objRQ_MstSv_Sys_User.NetworkID ?? ""));

            // Nguồn: MasterServer.cs:548-554 — MstSv_Mst_Network_CheckDB(NetworkID, Flag.Yes, Flag.Active).
            var drNetwork = await MstSv_Mst_Network_CheckDBAsync(
                alParamsCoupleError,
                objRQ_MstSv_Sys_User.NetworkID,
                MgErr.FlagActive,      // strFlagExistToCheck = Flag.Yes == Flag.Active == "1"
                MgErr.FlagActive);     // strFlagActiveListToCheck = Flag.Active == "1"

            // Nguồn: MasterServer.cs:556 — SetRemark(dtDB_MstSv_Mst_Network.Rows[0]["WSUrlAddr"]).
            objRT_MstSv_Sys_User = new MgRT_MstSv_Sys_User();
            objRT_MstSv_Sys_User.c_K_DT_Sys = BuildSys(
                objRQ_MstSv_Sys_User.Tid,
                errorCode: null,
                remark: drNetwork.WSUrlAddr);
            #endregion

            // Return Good: (nguồn MasterServerController.cs:70-72)
            return MgJsonResult(Success(objRT_MstSv_Sys_User));
        }
        catch (Exception ex)
        {
            #region // Catch of try: (nguồn MasterServerController.cs:73-89)
            var errCode = ex is MgServiceException sx ? sx.ErrorCode : strErrorCodeDefault;
            objRT_MstSv_Sys_User ??= new MgRT_MstSv_Sys_User();
            objRT_MstSv_Sys_User.c_K_DT_Sys = BuildSys(
                objRQ_MstSv_Sys_User.Tid,
                errorCode: errCode,
                remark: null,
                alParamsCoupleError.ToArray());
            // Nguồn dùng quá tải Error<T>(ex, Data) -> Exception = null (xem MgApiControllerBase).
            return MgJsonResult(Error(ex, objRT_MstSv_Sys_User));
            #endregion
        }
    }

    /// <summary>
    /// Port 1:1 idN.MobileGate.Biz/MasterServer.cs:1382-1446 MstSv_Mst_Network_CheckDB.
    /// SQL nguồn (dòng 1391-1397): select top 1 t.* from MstSv_Mst_Network t where t.NetworkID = @objNetworkID.
    ///
    /// Trình tự kiểm 1:1:
    ///   a) strFlagExistToCheck == Flag.Active ("1") và KHÔNG có dòng
    ///      -> MstSv_Mst_Network_CheckDB_NetworkNotFound        (nguồn dòng 1408-1417)
    ///   b) strFlagExistToCheck == Flag.Inactive ("0") và CÓ dòng
    ///      -> MstSv_Mst_Network_CheckDB_NetworkExist           (nguồn dòng 1419-1429)
    ///   c) strFlagActiveListToCheck khác rỗng và KHÔNG chứa FlagActive của dòng
    ///      -> MstSv_Mst_Network_CheckDB_FlagActiveNotMatched   (nguồn dòng 1432-1445)
    ///
    /// LƯU Ý 1:1 ở (c): nguồn viết strFlagActiveListToCheck.Contains(FlagActive-của-dòng),
    /// tức là "danh sách CHỨA giá trị", nên nếu FlagActive trong DB là chuỗi RỖNG thì
    /// "1".Contains("") == true và kiểm này ĐI QUA. Giữ nguyên chiều so sánh này.
    /// </summary>
    private async Task<MstSvNetwork> MstSv_Mst_Network_CheckDBAsync(
        List<(string PCode, string PVal)> alParamsCoupleError,
        string? objNetworkID,
        string strFlagExistToCheck,
        string strFlagActiveListToCheck)
    {
        // Nguồn: select top 1 ... where t.NetworkID = @objNetworkID (không lọc FlagActive trong SQL).
        var row = await _db.MstSvNetworks.FirstOrDefaultAsync(t => t.NetworkID == objNetworkID);

        // (a) + (b) — nguồn dòng 1406-1430.
        if (strFlagExistToCheck.Length > 0)
        {
            if (string.Equals(strFlagExistToCheck, MgErr.FlagActive, StringComparison.Ordinal) && row == null)
            {
                alParamsCoupleError.Add(("Check.NetworkID", objNetworkID ?? ""));
                throw new MgServiceException(MgErr.MstSv_Mst_Network_CheckDB_NetworkNotFound,
                    alParamsCoupleError.ToArray());
            }
            if (string.Equals(strFlagExistToCheck, MgErr.FlagInactive, StringComparison.Ordinal) && row != null)
            {
                alParamsCoupleError.Add(("Check.NetworkID", objNetworkID ?? ""));
                // Nguồn: Error.idN.TVANMasterServer.cs:76 MstSv_Mst_Network_CheckDB_NetworkExist.
                throw new MgServiceException("ErridNTVAN.MstSv_Mst_Network_CheckDB_NetworkExist",
                    alParamsCoupleError.ToArray());
            }
        }

        // (c) — nguồn dòng 1432-1445. Nguồn đọc Rows[0] không kiểm null vì (a) đã chặn.
        if (strFlagActiveListToCheck.Length > 0 && !strFlagActiveListToCheck.Contains(row!.FlagActive ?? ""))
        {
            alParamsCoupleError.Add(("Check.NetworkID", objNetworkID ?? ""));
            alParamsCoupleError.Add(("Check.strFlagActiveListToCheck", strFlagActiveListToCheck));
            alParamsCoupleError.Add(("DB.FlagActive", row.FlagActive ?? ""));
            throw new MgServiceException(MgErr.MstSv_Mst_Network_CheckDB_FlagActiveNotMatched,
                alParamsCoupleError.ToArray());
        }

        return row!;
    }
}
