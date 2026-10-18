using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniGate.Data;
using MiniGate.Models.Port;

namespace MiniGate.Controllers.Port;

// ============================================================================================
// PORT 1:1 — EcoreController/Inos_CheckUserExist (hạng mục #050) của dự án nguồn 2019.9.MobileGate.
//
// Nguồn controller (tầng gate):
//   EcoreController.cs:552-677         Inos_CheckUserExist -> gọi _biz.WAS_Inos_CheckUserExist_New20231115
// Nguồn biz:
//   Ecore.cs:794-892                   WAS_Inos_CheckUserExist_New20231115 (biz wrapper, mã lỗi TError.ErridNTVAN.WAS_Inos_GetUser)
//   Ecore.cs:678-792                   Inos_CheckUserExist_New20231115 (biz core: kiểm tra blacklist Mst_UserBlackList, kiểm tra format UserCode)
//   zTemp.cs:42607-42664               Mst_UserBlackList_CheckDB (truy vấn top 1 t.* from Mst_UserBlackList where UserCode = @objUserCode)
//   Ecore.cs:3453-3463                 PhoneNumber.IsPhoneNbr (regex kiểm tra số điện thoại motif)
// Nguồn model & hằng số:
//   idN.MobileGate.Common/Models/Ecore/RQ_InosUser.cs:10-20
//   idN.MobileGate.Common/Models/Ecore/RT_InosUser.cs:10-14
//   idN.MobileGate.Common/Models/WARQBase.cs:9-51
//   idN.MobileGate.Constants/Error.idN.Ecore.cs:30-37:
//     - TError.ErridNTVAN.WAS_Inos_GetUser = "ErridN.MobileGate.WAS_Inos_GetUser"
//     - TError.ErridNTVAN.Inos_CheckUserExist_UserCodeDeleted = "ErridN.MobileGate.Inos_CheckUserExist_UserCodeDeleted"
//     - TError.ErridNTVAN.Inos_CheckUserExist_UserCodeInvalid = "ErridN.MobileGate.Inos_CheckUserExist_UserCodeInvalid"
//   idN.MobileGate.Constants/Error.DMS.Chain.cs:26:
//     - TError.ErridNTVAN.CmSys_InvalidOutSite = "ErridN.MobileGate.CmSys_InvalidOutSite"
//
// GIỮ ĐÚNG NGUỒN & QUY ƯỚC:
//   * Route: POST /api/ecore/inos_checkuserexist (nhận [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]).
//   * Kiểm tra BlackList Mst_UserBlackList: nếu tìm thấy -> lỗi Inos_CheckUserExist_UserCodeDeleted.
//   * Phân loại UserCode theo email / số điện thoại / ID số: nếu không hợp lệ -> ngoại lệ FormatException -> WAS_Inos_GetUser.
//   * Gọi dịch vụ ngoài AccountService.GetUser (inOS): bỏ qua và ghi GAP theo quy ước (§P3, PORT_RULES_MiniGate.md §1 & §2.5).
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
}
