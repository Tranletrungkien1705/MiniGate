using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniGate.Data;
using MiniGate.Models.Port;

namespace MiniGate.Controllers.Port;

// ============================================================================================
// PORT 1:1 — EcoreController (hạng mục #050 Inos_CheckUserExist, #065 WA_Mst_UserBlackList_Save)
// của dự án nguồn 2019.9.MobileGate.
//
// Nguồn controller (tầng gate):
//   EcoreController.cs:552-677         Inos_CheckUserExist -> gọi _biz.WAS_Inos_CheckUserExist_New20231115
//   EcoreController.cs:2557-2694       WA_Mst_UserBlackList_Save -> gọi _biz.WAS_Mst_UserBlackList_Save
// Nguồn biz:
//   Ecore.cs:794-892                   WAS_Inos_CheckUserExist_New20231115 (biz wrapper, mã lỗi TError.ErridNTVAN.WAS_Inos_GetUser)
//   Ecore.cs:678-792                   Inos_CheckUserExist_New20231115 (biz core: kiểm tra blacklist Mst_UserBlackList, kiểm tra format UserCode)
//   zTemp.cs:42607-42664               Mst_UserBlackList_CheckDB (truy vấn top 1 t.* from Mst_UserBlackList where UserCode = @objUserCode)
//   zTemp.cs:42673-42761               WAS_Mst_UserBlackList_Save (biz wrapper, mã lỗi TError.ErridNTVAN.WAS_Mst_UserBlackList_Save)
//   zTemp.cs:42763-42900               Mst_UserBlackList_Save (biz transaction, mã lỗi TError.ErridNTVAN.Mst_UserBlackList_Save)
//   zTemp.cs:42902-43158               Mst_UserBlackList_SaveX (biz core: CheckDB, RequestToken iNOS, delete/insert Mst_UserBlackList)
//   Ecore.cs:3453-3463                 PhoneNumber.IsPhoneNbr (regex kiểm tra số điện thoại motif)
// Nguồn model & hằng số:
//   idN.MobileGate.Common/Models/Ecore/RQ_InosUser.cs:10-20
//   idN.MobileGate.Common/Models/Ecore/RT_InosUser.cs:10-14
//   idN.MobileGate.Common/Models/RQ_Mst_UserBlackList.cs:9-16
//   idN.MobileGate.Common/Models/RT_Mst_UserBlackList.cs:9-15
//   idN.MobileGate.Common/Models/WARQBase.cs:9-51
//   idN.MobileGate.Constants/Error.idN.Ecore.cs:30-37:
//     - TError.ErridNTVAN.WAS_Inos_GetUser = "ErridN.MobileGate.WAS_Inos_GetUser"
//     - TError.ErridNTVAN.Inos_CheckUserExist_UserCodeDeleted = "ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted"
//     - TError.ErridNTVAN.Inos_CheckUserExist_UserCodeInvalid = "ErridN.MobileGate.Inos_CheckUserExist_UserCodeInvalid"
//     - TError.ErridNTVAN.WAS_Mst_UserBlackList_Save = "ErridN.MobileGate.WAS_Mst_UserBlackList_Save"
//     - TError.ErridNTVAN.Mst_UserBlackList_Save = "ErridN.MobileGate.Mst_UserBlackList_Save"
//   idN.MobileGate.Constants/Error.DMS.Chain.cs:26:
//     - TError.ErridNTVAN.CmSys_InvalidOutSite = "ErridN.MobileGate.CmSys_InvalidOutSite"
//
// GIỮ ĐÚNG NGUỒN & QUY ƯỚC:
//   * Route Inos_CheckUserExist: POST /api/ecore/inos_checkuserexist.
//   * Route WA_Mst_UserBlackList_Save: POST /api/ecore/wa_mst_userblacklist_save.
//   * Nhận [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)].
//   * Kiểm tra BlackList Mst_UserBlackList: nếu tìm thấy -> lỗi Inos_CheckUserExist_UserCodeDeleted.
//   * Phân loại UserCode theo email / số điện thoại / ID số: nếu không hợp lệ -> ngoại lệ FormatException -> WAS_Inos_GetUser.
//   * Gọi dịch vụ ngoài AccountService.GetUser / AccountService.RequestToken (inOS): bỏ qua và ghi GAP theo quy ước (§P3, PORT_RULES_MiniGate.md §1 & §2.5).
// ============================================================================================

/// <summary>
/// DTO yêu cầu cho action Inos_CheckUserExist — port 1:1 từ RQ_InosUser.cs và WARQBase.cs.
/// </summary>
public class EcoreInosCheckUserExistReq
{
    public string? Tid { get; set; }                      // Transaction ID (EcoreController.cs:555, 586)
    public string? OrgID { get; set; }                    // Mã tổ chức (EcoreController.cs:590, 616)
    public string? NetworkID { get; set; }                // Mã mạng (EcoreController.cs:589)
    public string? TokenID { get; set; }                  // Mã token (WARQBase.cs:17)
    public string? AccessToken { get; set; }              // Token truy cập OAuth2 (EcoreController.cs:585)
    public string? RefreshToken { get; set; }             // Refresh token (WARQBase.cs:21)
    public string? UtcOffset { get; set; }                // Độ lệch múi giờ (WARQBase.cs:23)
    public string? GwUserCode { get; set; }               // Người dùng Gateway (EcoreController.cs:583)
    public string? GwPassword { get; set; }               // Mật khẩu Gateway (EcoreController.cs:584)
    public string? WAUserCode { get; set; }               // Người dùng WA (EcoreController.cs:578, 587)
    public string? WAUserPassword { get; set; }           // Mật khẩu người dùng WA (WARQBase.cs:31)
    public string? WAUserCodeActual { get; set; }         // Người dùng thực tế (WARQBase.cs:33)
    public string? FlagIsDelete { get; set; }             // Cờ đã xoá (WARQBase.cs:35)
    public string? FlagAppr { get; set; }                 // Cờ phê duyệt (WARQBase.cs:36)
    public string? FuncType { get; set; }                 // Loại chức năng (WARQBase.cs:38)
    public string? Ft_RecordStart { get; set; }           // Phân trang bắt đầu (EcoreController.cs:579)
    public string? Ft_RecordCount { get; set; }           // Phân trang số dòng (EcoreController.cs:580)
    public string? Ft_WhereClause { get; set; }           // Mệnh đề where lọc (WARQBase.cs:44)
    public string? Ft_Cols_Upd { get; set; }              // Cột cập nhật (WARQBase.cs:46)
    public string? pageIndex { get; set; }                // Trang bắt đầu (EcoreController.cs:579)
    public string? pageSize { get; set; }                 // Kích thước trang (EcoreController.cs:580)

    public string? Email { get; set; }                    // Email nếu có (RQ_InosUser.cs:12)
    public string? Code { get; set; }                     // Mã kích hoạt / reset (RQ_InosUser.cs:14)
    public string? Password { get; set; }                 // Mật khẩu mới (RQ_InosUser.cs:15)
    public string? UserCode { get; set; }                 // Mã / email / phone người dùng cần kiểm tra (RQ_InosUser.cs:17)
    public string? Id { get; set; }                       // ID người dùng (RQ_InosUser.cs:18)
}

/// <summary>
/// DTO đại diện cho InosUser trả về trong phong bì RT_InosUser — port 1:1 từ RT_InosUser.cs:12.
/// </summary>
public class InosUserDto
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public long? Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// Kết quả nghiệp vụ trả về — port 1:1 từ RT_InosUser.cs:10-14 và WARTBase.cs:9-15.
/// </summary>
public class RT_InosUser
{
    public InosUserDto? InosUser { get; set; }
    public c_K_DT_Sys c_K_DT_Sys { get; set; } = new();
}

/// <summary>
/// DTO phản hồi trả về cho client — chứa Success và Data (RT_InosUser).
/// </summary>
public class EcoreInosCheckUserExistResp
{
    public bool Success { get; set; } = true;
    public RT_InosUser? Data { get; set; }
}

/// <summary>
/// DTO chứa thông tin dòng UserBlackList từ client — port 1:1 từ model Mst_UserBlackList.cs.
/// </summary>
public class Mst_UserBlackListInputDto
{
    public string? UserCode { get; set; }
    public string? NetworkID { get; set; }
    public string? ProviderCode { get; set; }
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }
    public string? PhoneNo { get; set; }
    public string? EMail { get; set; }
    public string? UUID { get; set; }
    public string? FlagActive { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action WA_Mst_UserBlackList_Save — port 1:1 từ RQ_Mst_UserBlackList.cs và WARQBase.cs.
/// </summary>
public class EcoreUserBlackListSaveReq
{
    public string? Tid { get; set; }                      // Transaction ID (EcoreController.cs:2560, 2597)
    public string? OrgID { get; set; }                    // Mã tổ chức (EcoreController.cs:2602, 2627)
    public string? NetworkID { get; set; }                // Mã mạng (EcoreController.cs:2601)
    public string? TokenID { get; set; }                  // Mã token (WARQBase.cs:17)
    public string? AccessToken { get; set; }              // Token truy cập OAuth2 (EcoreController.cs:2580, 2596)
    public string? RefreshToken { get; set; }             // Refresh token (WARQBase.cs:21)
    public string? UtcOffset { get; set; }                // Độ lệch múi giờ (WARQBase.cs:23)
    public string? GwUserCode { get; set; }               // Người dùng Gateway (EcoreController.cs:2594)
    public string? GwPassword { get; set; }               // Mật khẩu Gateway (EcoreController.cs:2595)
    public string? WAUserCode { get; set; }               // Người dùng WA (EcoreController.cs:2584, 2598)
    public string? WAUserPassword { get; set; }           // Mật khẩu người dùng WA (WARQBase.cs:31)
    public string? WAUserCodeActual { get; set; }         // Người dùng thực tế (WARQBase.cs:33)
    public string? FlagIsDelete { get; set; }             // Cờ đã xoá (EcoreController.cs:2599, zTemp.cs:42951)
    public string? FlagAppr { get; set; }                 // Cờ phê duyệt (WARQBase.cs:36)
    public string? FuncType { get; set; }                 // Loại chức năng (WARQBase.cs:38)
    public string? Ft_RecordStart { get; set; }           // Phân trang bắt đầu (WARQBase.cs:40)
    public string? Ft_RecordCount { get; set; }           // Phân trang số dòng (WARQBase.cs:42)
    public string? Ft_WhereClause { get; set; }           // Mệnh đề where lọc (WARQBase.cs:44)
    public string? Ft_Cols_Upd { get; set; }              // Cột cập nhật (WARQBase.cs:46)
    public string? Rt_Cols_Mst_UserBlackList { get; set; } // Danh sách cột trả về (RQ_Mst_UserBlackList.cs:11)

    public Mst_UserBlackListInputDto? Mst_UserBlackList { get; set; } // Thông tin người dùng (RQ_Mst_UserBlackList.cs:13)
}

/// <summary>
/// Kết quả nghiệp vụ trả về cho WA_Mst_UserBlackList_Save — port 1:1 từ RT_Mst_UserBlackList.cs:9-14 và WARTBase.cs:9-15.
/// </summary>
public class RT_Mst_UserBlackList
{
    public List<Mst_UserBlackList>? Lst_Mst_UserBlackList { get; set; }
    public c_K_DT_Sys c_K_DT_Sys { get; set; } = new();
}

/// <summary>
/// DTO phản hồi trả về cho client cho action WA_Mst_UserBlackList_Save — chứa Success và Data (RT_Mst_UserBlackList).
/// </summary>
public class EcoreUserBlackListSaveResp
{
    public bool Success { get; set; } = true;
    public RT_Mst_UserBlackList? Data { get; set; }
}

/// <summary>
/// Lớp tiện ích kiểm tra định dạng số điện thoại theo motif nguồn Ecore.cs:3453-3463.
/// </summary>
public static class PhoneNumberHelper
{
    public const string Motif = @"^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$";

    public static bool IsPhoneNbr(string? number)
    {
        if (number != null) return Regex.IsMatch(number, Motif);
        return false;
    }
}

/// <summary>
/// Controller Port 1:1 cho EcoreController (hạng mục #050).
/// </summary>
[ApiController]
[Route("api/ecore")]
public class EcoreControllerPort : ControllerBase
{
    /// <summary>
    /// Port 1:1 action Inos_CheckUserExist (EcoreController.cs:552-677).
    /// Kiểm tra người dùng có tồn tại trong hệ thống và không nằm trong danh sách đen Mst_UserBlackList.
    /// </summary>
    [HttpPost("inos_checkuserexist")]
    public async Task<IActionResult> Inos_CheckUserExist(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EcoreInosCheckUserExistReq? req,
        [FromServices] AppDbContext db)
    {
        // Nhận tham số header theo nguồn EcoreController.cs:571-590
        var headerOrgId = Request?.Headers["orgId"].FirstOrDefault();
        var headerNetworkId = Request?.Headers["networkId"].FirstOrDefault();
        var headerGwUserCode = Request?.Headers["GwUserCode"].FirstOrDefault();
        var headerGwPassword = Request?.Headers["GwPassword"].FirstOrDefault();
        var headerAuth = Request?.Headers["Authorization"].FirstOrDefault();

        string? accessToken = req?.AccessToken;
        if (string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(headerAuth) && headerAuth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            accessToken = headerAuth.Substring(7);
        }

        // 1. Kiểm tra body rỗng theo nguồn:
        // EcoreController.cs:555 truy cập objRQ_InosUser.Tid khi null ném ngoại lệ -> mã mặc định WAS_Inos_GetUser (Ecore.cs:808)
        if (req == null)
        {
            return PortResult.Fail("WAS_Inos_GetUser", "Exception", "Request body cannot be null", "Check.req", null);
        }

        var userCode = req.UserCode;
        if (string.IsNullOrWhiteSpace(userCode))
        {
            // Convert.ToDouble("") ném FormatException -> WAS_Inos_CheckUserExist_New20231115 bắt và gán WAS_Inos_GetUser (Ecore.cs:808, 869-878)
            return PortResult.Fail("WAS_Inos_GetUser", "Exception", "UserCode is required and cannot be empty", "Check.UserCode", userCode);
        }

        // 2. Chuẩn hoá và kiểm tra định dạng objUserCode theo nguồn Ecore.cs:703-720:
        // - Email: new EmailAddressAttribute().IsValid(objUserCode)
        // - Số điện thoại: PhoneNumber.IsPhoneNbr(objUserCode) với motif ^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$
        // - ID số: Convert.ToDouble(objUserCode)
        // Nếu không thuộc 3 dạng trên: Convert.ToDouble ném FormatException -> lỗi WAS_Inos_GetUser (Ecore.cs:808, 869-878).
        string? strEmail = null;
        string? strPhone = null;
        long lgId = 0;

        if (new EmailAddressAttribute().IsValid(userCode))
        {
            strEmail = userCode;
        }
        else if (PhoneNumberHelper.IsPhoneNbr(userCode))
        {
            strPhone = userCode;
        }
        else
        {
            try
            {
                lgId = (long)Convert.ToDouble(userCode);
            }
            catch (Exception ex)
            {
                // Nguồn: Ecore.cs:808, 869-878 (strErrorCodeDefault = TError.ErridNTVAN.WAS_Inos_GetUser)
                return PortResult.Fail("WAS_Inos_GetUser", "Exception", ex.Message, "Check.UserCode", userCode);
            }
        }

        // 3. Kiểm tra danh sách đen Mst_UserBlackList theo nguồn Ecore.cs:722-744 và zTemp.cs:42607-42655:
        // SQL nguồn: select top 1 t.* from Mst_UserBlackList t where (1=1) and t.UserCode = @objUserCode;
        // Nếu đã tồn tại trong BlackList -> ném TError.ErridNTVAN.Inos_CheckUserExist_UserCodeDeleted ("ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted")
        var userCodePattern = PortQuery.Equal(userCode);
        var inBlackList = await db.Set<Mst_UserBlackList>()
            .AnyAsync(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), userCodePattern, PortQuery.Esc));

        if (inBlackList)
        {
            return PortResult.Fail("Inos_CheckUserExist_UserCodeDeleted", "Check.UserCode", userCode);
        }

        // 4. Lệnh gọi dịch vụ ngoài inOS (Ecore.cs:747-791):
        // AccountService objAccountService = new AccountService(null);
        // objAccountService.AccessToken = strAccessToken;
        // InosUser ret = objAccountService.GetUser(dummy);
        // Đích không có dịch vụ inOS -> BỎ QUA và ghi GAP theo quy ước (§P3, PORT_RULES_MiniGate.md §1 & §2.5).

        // 5. Kết quả thành công theo nguồn EcoreController.cs:642-644:
        // resultEntry.Data = objRT_InosUser; (với objRT_InosUser = new RT_InosUser())
        var tid = !string.IsNullOrWhiteSpace(req.Tid)
            ? req.Tid
            : DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff");

        var rtInosUser = new RT_InosUser
        {
            InosUser = null,
            c_K_DT_Sys = new c_K_DT_Sys
            {
                Lst_c_K_DT_SysInfo = new List<c_K_DT_SysInfo>
                {
                    new c_K_DT_SysInfo
                    {
                        Tid = tid,
                        ErrorCode = "0",
                        FlagWarning = "0",
                        Remark = "Inos_CheckUserExist Success"
                    }
                },
                Lst_c_K_DT_SysError = new List<c_K_DT_SysError>(),
                Lst_c_K_DT_SysWarning = new List<c_K_DT_SysWarning>()
            }
        };

        return Ok(new EcoreInosCheckUserExistResp
        {
            Success = true,
            Data = rtInosUser
        });
    }

    /// <summary>
    /// Chuẩn hoá chuỗi tham số dạng mã: Trim, ToUpper, rỗng hoặc khoảng trắng trả về null (PORT_RULES_MiniGate.md §3).
    /// </summary>
    private static string? StdParam(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Port 1:1 action WA_Mst_UserBlackList_Save (EcoreController.cs:2557-2694).
    /// Thêm / cập nhật (khi FlagIsDelete != "1") hoặc xoá (khi FlagIsDelete == "1")
    /// người dùng khỏi danh sách đen Mst_UserBlackList.
    /// </summary>
    [HttpPost("wa_mst_userblacklist_save")]
    public async Task<IActionResult> WA_Mst_UserBlackList_Save(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EcoreUserBlackListSaveReq? req,
        [FromServices] AppDbContext db,
        [FromServices] IConfiguration config)
    {
        // 1. Kiểm tra body rỗng theo nguồn:
        // zTemp.cs:42681 objRQ_Mst_UserBlackList.Tid hoặc line 42725 objRQ_Mst_UserBlackList.Mst_UserBlackList.UserCode ném NullReferenceException -> TError.ErridNTVAN.WAS_Mst_UserBlackList_Save
        if (req == null || req.Mst_UserBlackList == null)
        {
            return PortResult.Fail("WAS_Mst_UserBlackList_Save", "Exception", "Request body and Mst_UserBlackList cannot be null", "Check.req", null);
        }

        // Lấy headers nếu có theo nguồn EcoreController.cs:2576-2602
        var headerOrgId = Request?.Headers["orgId"].FirstOrDefault();
        var headerNetworkId = Request?.Headers["networkId"].FirstOrDefault();
        var headerGwUserCode = Request?.Headers["GwUserCode"].FirstOrDefault();
        var headerGwPassword = Request?.Headers["GwPassword"].FirstOrDefault();
        var headerAuth = Request?.Headers["Authorization"].FirstOrDefault();
        var headerWAUserCode = Request?.Headers["WAUserCode"].FirstOrDefault();

        var waUserCode = !string.IsNullOrEmpty(req.WAUserCode)
            ? req.WAUserCode
            : !string.IsNullOrEmpty(headerWAUserCode)
                ? headerWAUserCode
                : "port";

        var networkId = !string.IsNullOrEmpty(req.NetworkID)
            ? req.NetworkID
            : !string.IsNullOrEmpty(headerNetworkId)
                ? headerNetworkId
                : config["Biz_NetworkID"] ?? "1001";

        var tid = !string.IsNullOrWhiteSpace(req.Tid)
            ? req.Tid
            : DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff");

        // Chuẩn hoá input theo nguồn zTemp.cs:42951-42960:
        // bool bIsDelete = CmUtils.StringUtils.StringEqual(objFlagIsDelete, TConst.Flag.Yes);
        bool bIsDelete = string.Equals(req.FlagIsDelete, "1", StringComparison.Ordinal);

        // string strUserCode = TUtils.CUtils.StdParam(objUserCode);
        // string strProviderCode = TUtils.CUtils.StdParam(objProviderCode);
        // string strUserName = string.Format("{0}", objUserName);
        // string strUserPassword = string.Format("{0}", objUserPassword);
        // string strPhoneNo = string.Format("{0}", objPhoneNo);
        // string strEMail = string.Format("{0}", objEMail);
        // string strUUID = string.Format("{0}", objUUID);
        var strUserCode = StdParam(req.Mst_UserBlackList.UserCode);
        var strProviderCode = StdParam(req.Mst_UserBlackList.ProviderCode);
        var strUserName = string.Format("{0}", req.Mst_UserBlackList.UserName ?? "");
        var strUserPassword = string.Format("{0}", req.Mst_UserBlackList.UserPassword ?? "");
        var strPhoneNo = string.Format("{0}", req.Mst_UserBlackList.PhoneNo ?? "");
        var strEMail = string.Format("{0}", req.Mst_UserBlackList.EMail ?? "");
        var strUUID = string.Format("{0}", req.Mst_UserBlackList.UUID ?? "");

        DateTime dtimeSys = DateTime.UtcNow;

        // Transaction theo nguồn zTemp.cs:42808 (_cf.db.BeginTransaction())
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // Mst_UserBlackList_CheckDB theo nguồn zTemp.cs:42969-42987:
            Mst_UserBlackList? existing = null;
            if (!string.IsNullOrEmpty(strUserCode))
            {
                var userCodePattern = PortQuery.Equal(strUserCode);
                existing = await db.Set<Mst_UserBlackList>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), userCodePattern, PortQuery.Esc));
            }

            if (existing == null) // Chưa tồn tại
            {
                if (bIsDelete)
                {
                    // zTemp.cs:42981 goto MyCodeLabel_Done; (Thành công ngay khi xoá bản ghi không tồn tại)
                    await tx.CommitAsync();
                    return Ok(new EcoreUserBlackListSaveResp
                    {
                        Success = true,
                        Data = new RT_Mst_UserBlackList
                        {
                            Lst_Mst_UserBlackList = null,
                            c_K_DT_Sys = new c_K_DT_Sys
                            {
                                Lst_c_K_DT_SysInfo = new List<c_K_DT_SysInfo>
                                {
                                    new c_K_DT_SysInfo
                                    {
                                        Tid = tid,
                                        ErrorCode = "0",
                                        FlagWarning = "0",
                                        Remark = "Mst_UserBlackList_Save Success"
                                    }
                                }
                            }
                        }
                    });
                }
            }

            // Nếu không phải xoá mà strUserCode lại null/rỗng:
            // UserCode là PRIMARY KEY, không thể null khi tạo/sửa -> lỗi Mst_UserBlackList_Save
            if (!bIsDelete && string.IsNullOrEmpty(strUserCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_UserBlackList_Save", "Check.UserCode", null, "Exception", "UserCode is required");
            }

            // Gọi dịch vụ ngoài iNOS AccountService.RequestToken (zTemp.cs:42996-43023):
            // BỎ QUA và ghi GAP theo quy ước (§P3, PORT_RULES_MiniGate.md §1 & §2.5).

            // Xoá bản ghi cũ nếu có (zTemp.cs:43069-43080)
            if (existing != null)
            {
                db.Set<Mst_UserBlackList>().Remove(existing);
                await db.SaveChangesAsync();
            }

            // Nếu không phải thao tác xoá (bIsDelete == false) -> Thêm mới bản ghi (zTemp.cs:43084-43133)
            if (!bIsDelete)
            {
                var newItem = new Mst_UserBlackList
                {
                    UserCode = strUserCode!,
                    NetworkID = networkId,
                    ProviderCode = strProviderCode,
                    UserName = strUserName,
                    UserPassword = strUserPassword,
                    PhoneNo = strPhoneNo,
                    EMail = strEMail,
                    UUID = strUUID,
                    FlagActive = "1", // TConst.Flag.Active
                    LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
                    LogLUBy = waUserCode
                };
                db.Set<Mst_UserBlackList>().Add(newItem);
                await db.SaveChangesAsync();
            }

            await tx.CommitAsync();

            return Ok(new EcoreUserBlackListSaveResp
            {
                Success = true,
                Data = new RT_Mst_UserBlackList
                {
                    Lst_Mst_UserBlackList = null,
                    c_K_DT_Sys = new c_K_DT_Sys
                    {
                        Lst_c_K_DT_SysInfo = new List<c_K_DT_SysInfo>
                        {
                            new c_K_DT_SysInfo
                            {
                                Tid = tid,
                                ErrorCode = "0",
                                FlagWarning = "0",
                                Remark = "Mst_UserBlackList_Save Success"
                            }
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Lỗi trong transaction: Mst_UserBlackList_Save (zTemp.cs:42877)
            return PortResult.Fail("Mst_UserBlackList_Save", "Exception", ex.Message);
        }
    }
}
