using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MiniGate.Data;

namespace MiniGate.Controllers.Port;

// ============================================================================================
// PORT 1:1 — INOSController (dự án nguồn 2019.9.MobileGate) — dữ liệu nằm ở dịch vụ NGOÀI iNOS.
//
// Hạng mục #483 — GetUserPermissions:
//   INOSController.cs:655-776   GetUserPermissions -> _biz.WAS_Inos_Permissions_GetUserPermissions
//   OS.Inos.cs:4005-4064        Inos_Permissions_GetUserPermissions -> UserService.GetUserPermissions (ngoài)
// Hạng mục #478 — GetCurrentUserSolutions (MỤC NÀY):
//   INOSController.cs:33-154    GetCurrentUserSolutions -> _biz.WAS_Inos_LicService_GetCurrentUserSolution
//   DMS.cs:8982-9060            WAS_Inos_LicService_GetCurrentUserSolution -> Inos_LicService_GetCurrentUserSolution
//                               (KHÔNG có định nghĩa trong nguồn trích — 'BIZ: khong thay dinh nghia
//                                Inos_LicService_GetCurrentUserSolution', source_excerpt.txt:460)
//   Mã lỗi mặc định của biz:    TError.ErridNTVAN.WAS_OS_Inos_OrderService_GetDiscountCode (DMS.cs:8999)
//
// Cả hai hạng mục: TOÀN BỘ DỮ LIỆU NẰM Ở DỊCH VỤ NGOÀI iNOS. Gate chỉ kiểm token qua iNOS
// AccountService (ApiControllerBase.cs:85-125), biz chỉ gọi dịch vụ ngoài rồi trả về. Đích không có
// dịch vụ iNOS ⇒ KHÔNG mô phỏng, KHÔNG tự chế bảng/kết quả (PORT_RULES_MiniGate.md §1, §2.5;
// LESSONS_AGENTS.md B.7). Xem .parity-run\conflicts\478_conflict.md và NO_BACKEND.flag.
//
// QUY ƯỚC CHUNG:
//   * Route: POST /api/inos/<action viết thường> ([FromBody(EmptyBodyBehavior = Allow)]).
//   * CheckAuthorize (ApiControllerBase.cs:85-125): orgId rỗng -> false; token rỗng -> false;
//     còn lại gọi dịch vụ ngoài iNOS AccountService.GetCurrentUser -> GAP (đích không có iNOS).
//     Khi false -> { Success = false, Message = "Authorize is false", Detail = "Authorize is false" }
//     (HTTP 200 như nguồn Json(data, AllowGet)).
//   * Nhánh gọi dịch vụ ngoài -> 400 PortResult.Fail với mã lỗi mặc định của biz + cặp "Check.strAccessToken".
// ============================================================================================

// ---------------------------------------------------------------------------
// #483 GetUserPermissions
// ---------------------------------------------------------------------------

/// <summary>
/// DTO yêu cầu cho action GetUserPermissions — port 1:1 từ RQ_OrgUser
/// (INOSController.cs:655, 682-692; OS.Inos.cs:4105-4113). Chỉ khai các trường gate/biz THỰC SỰ đọc.
/// </summary>
public class INOSGetUserPermissionsReq
{
    public string? Tid { get; set; }              // Transaction ID (INOSController.cs:659, 688)
    public string? WAUserCode { get; set; }       // Người dùng WA (INOSController.cs:682, 689; OS.Inos.cs:4119)
    public string? WAUserPassword { get; set; }   // Mật khẩu người dùng WA (OS.Inos.cs:4120)
    public int? pageIndex { get; set; }           // -> Ft_RecordStart (INOSController.cs:683)
    public int? pageSize { get; set; }            // -> Ft_RecordCount (INOSController.cs:684)
    public string? UserId { get; set; }           // OrgUser.UserId (OS.Inos.cs:4107)
    public string? Inos_OrgId { get; set; }       // OrgUser.OrgId (OS.Inos.cs:4108)
    public string? Name { get; set; }             // OrgUser.Name (OS.Inos.cs:4109)
    public string? Email { get; set; }            // OrgUser.Email (OS.Inos.cs:4110)
    public string? Status { get; set; }           // OrgUser.Status (OS.Inos.cs:4111)
    public string? Role { get; set; }             // OrgUser.Role (OS.Inos.cs:4112)
}

/// <summary>
/// Kết quả nghiệp vụ — port 1:1 từ RT_InosPermissions (OS.Inos.cs:4076, 4127).
/// Kiểu phần tử InosPermission thuộc dịch vụ ngoài iNOS (không có định nghĩa trong nguồn trích) ⇒ object.
/// </summary>
public class RT_InosPermissions
{
    public List<object>? Lst_InosPermission { get; set; }
}

/// <summary>
/// DTO phản hồi trả về cho client — chứa Success và Data (RT_InosPermissions).
/// </summary>
public class INOSGetUserPermissionsResp
{
    public bool Success { get; set; } = true;
    public RT_InosPermissions? Data { get; set; }
}

// ---------------------------------------------------------------------------
// #478 GetCurrentUserSolutions
// ---------------------------------------------------------------------------

/// <summary>
/// DTO yêu cầu cho action GetCurrentUserSolutions — port 1:1 từ RQ_Inos_LicOrgSolution
/// (INOSController.cs:33, 60-67; DMS.cs:8982-9030). Chỉ khai trường client gửi; các trường
/// GwUserCode/GwPassword/AccessToken/OrgID/Ft_RecordStart/Ft_RecordCount do gate gán từ header/cấu hình
/// (INOSController.cs:61-70) — không nhận từ body.
/// </summary>
public class INOSGetCurrentUserSolutionsReq
{
    public string? Tid { get; set; }                 // Transaction ID (INOSController.cs:37, 66)
    public string? WAUserCode { get; set; }          // Người dùng WA (INOSController.cs:60, 67; DMS.cs:9023)
    public string? WAUserPassword { get; set; }      // Mật khẩu người dùng WA (DMS.cs:9024)
    public int? pageIndex { get; set; }              // -> Ft_RecordStart (INOSController.cs:61)
    public int? pageSize { get; set; }               // -> Ft_RecordCount (INOSController.cs:62)
    public string? Inos_LicOrgSolution { get; set; } // strRt_Cols_OS_Inos_LicOrder (DMS.cs:9029) — chuỗi chọn cột
}

/// <summary>
/// Kết quả nghiệp vụ — port 1:1 từ RT_Inos_LicOrgSolution (DMS.cs:8991, 9031).
/// Kiểu phần tử Inos_LicOrgSolution thuộc dịch vụ ngoài iNOS (không có định nghĩa trong nguồn trích) ⇒ object.
/// </summary>
public class RT_Inos_LicOrgSolution
{
    public List<object>? Lst_Inos_LicOrgSolution { get; set; }
}

/// <summary>
/// DTO phản hồi trả về cho client — chứa Success và Data (RT_Inos_LicOrgSolution).
/// </summary>
public class INOSGetCurrentUserSolutionsResp
{
    public bool Success { get; set; } = true;
    public RT_Inos_LicOrgSolution? Data { get; set; }
}

/// <summary>
/// Controller Port 1:1 cho INOSController (hạng mục #483 + #478).
/// </summary>
[ApiController]
[Route("api/inos")]
public class INOSControllerPort : ControllerBase
{
    /// <summary>
    /// Port 1:1 action GetUserPermissions (INOSController.cs:655-776).
    /// Lấy danh sách quyền của người dùng từ dịch vụ ngoài iNOS (GAP — không mô phỏng).
    /// </summary>
    [HttpPost("getuserpermissions")]
    public IActionResult GetUserPermissions(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] INOSGetUserPermissionsReq? req)
    {
        // 1. Refine and Check Token (INOSController.cs:675-680): Authorization: Bearer <token>
        var strVal = Request?.Headers["Authorization"].FirstOrDefault();
        string? strAuthorization = null;
        if (!string.IsNullOrEmpty(strVal) && strVal.StartsWith("Bearer "))
        {
            strAuthorization = strVal.Substring(7);
        }

        // 2. Header orgId (INOSController.cs:692, 719)
        var strOrgID = Request?.Headers["orgId"].FirstOrDefault();

        // 3. CheckAuthorize (INOSController.cs:697-717 -> ApiControllerBase.cs:85-125):
        //    orgId rỗng -> false; token rỗng -> false; còn lại gọi dịch vụ ngoài iNOS AccountService.GetCurrentUser
        //    (ApiControllerBase.cs:109-114) -> GAP: đích không có dịch vụ iNOS nên KHÔNG xác thực được token.
        var checkAuthorize = !string.IsNullOrEmpty(strOrgID) && !string.IsNullOrEmpty(strAuthorization);

        if (!checkAuthorize)
        {
            // INOSController.cs:703-709
            return Ok(new
            {
                Success = false,
                Message = "Authorize is false",
                Detail = "Authorize is false"
            });
        }

        // 4. CheckGatewayAuthentication (INOSController.cs:724-729) — bỏ qua theo quy ước chung của môi trường port.

        // 5. _biz.WAS_Inos_Permissions_GetUserPermissions (INOSController.cs:732 -> OS.Inos.cs:4067-4156)
        //    -> Inos_Permissions_GetUserPermissions (OS.Inos.cs:4005-4064) -> UserService.GetUserPermissions(objOrgUser)
        //    (OS.Inos.cs:4040-4043): TOÀN BỘ dữ liệu nằm ở dịch vụ ngoài iNOS -> GAP, KHÔNG mô phỏng.
        //    Nguồn: lỗi dịch vụ ném TError.ErridNTVAN.Inos_LicService_GetCurrentUserSolutions (OS.Inos.cs:4057-4061),
        //    biz bọc lại bằng mã lỗi mặc định TError.ErridNTVAN.WAS_OS_Inos_OrderService_GetDiscountCode (OS.Inos.cs:4084),
        //    gate trả Success = false (INOSController.cs:745-773).
        return PortResult.Fail(
            "WAS_OS_Inos_OrderService_GetDiscountCode",
            "Check.strAccessToken", strAuthorization);
    }

    /// <summary>
    /// Port 1:1 action GetCurrentUserSolutions (INOSController.cs:33-154, hạng mục #478).
    /// Dữ liệu (danh sách solution của user) chỉ có ở dịch vụ ngoài iNOS -> GAP, không mô phỏng.
    /// </summary>
    [HttpPost("getcurrentusersolutions")]
    public IActionResult GetCurrentUserSolutions(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] INOSGetCurrentUserSolutionsReq? req)
    {
        // 1. Refine and Check Token (INOSController.cs:52-58): Authorization: Bearer <token>
        var strVal = Request?.Headers["Authorization"].FirstOrDefault();
        string? strAuthorization = null;
        if (!string.IsNullOrEmpty(strVal) && strVal.StartsWith("Bearer "))
        {
            strAuthorization = strVal.Substring(7);
        }

        // 2. Header orgId (INOSController.cs:69-70, 97)
        var strOrgID = Request?.Headers["orgId"].FirstOrDefault();

        // 3. CheckAuthorize (INOSController.cs:74-95 -> ApiControllerBase.cs:85-125):
        //    orgId rỗng -> false; token rỗng -> false; còn lại gọi dịch vụ ngoài iNOS AccountService.GetCurrentUser
        //    (ApiControllerBase.cs:109-114) -> GAP: đích không có dịch vụ iNOS nên KHÔNG xác thực được token.
        var checkAuthorize = !string.IsNullOrEmpty(strOrgID) && !string.IsNullOrEmpty(strAuthorization);

        if (!checkAuthorize)
        {
            // INOSController.cs:80-87
            return Ok(new
            {
                Success = false,
                Message = "Authorize is false",
                Detail = "Authorize is false"
            });
        }

        // 4. CheckGatewayAuthentication (INOSController.cs:101-108) — bỏ qua theo quy ước chung của môi trường port.

        // 5. _biz.WAS_Inos_LicService_GetCurrentUserSolution (INOSController.cs:110 -> DMS.cs:8982-9060)
        //    -> Inos_LicService_GetCurrentUserSolution (KHÔNG có định nghĩa trong nguồn trích, source_excerpt.txt:460;
        //       gọi dịch vụ ngoài iNOS) -> TOÀN BỘ dữ liệu nằm ở dịch vụ ngoài iNOS -> GAP, KHÔNG mô phỏng.
        //    Biz bọc lỗi bằng mã mặc định TError.ErridNTVAN.WAS_OS_Inos_OrderService_GetDiscountCode (DMS.cs:8999, 9041-9046),
        //    gate trả Success = false (INOSController.cs:123-151).
        return PortResult.Fail(
            "WAS_OS_Inos_OrderService_GetDiscountCode",
            "Check.strAccessToken", strAuthorization);
    }
}
