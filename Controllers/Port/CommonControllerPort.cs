using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MiniGate.Controllers.Port;

// ============================================================================================
// PORT 1:1 — CommonController/WA_Common_GetDTime (hạng mục #011) của dự án nguồn 2019.9.MobileGate.
//
// Nguồn controller (tầng gate):
//   CommonController.cs:23-181         WA_Common_GetDTime -> gọi _biz.WAS_Cm_GetDTime
// Nguồn biz:
//   Common.cs:1131-1186                WAS_Cm_GetDTime (biz wrapper, mã lỗi ErridN.MobileGate.WAS_Cm_GetDTime)
//   Common.cs:755-766                  Cm_GetDTime (biz core: gán DateTime.UtcNow định dạng "yyyy-MM-dd HH:mm:ss.ffffff")
// Nguồn model & hằng số:
//   idN.MobileGate.Common/Models/Common.cs:9-12 (class Common rỗng, 0 thuộc tính, không bảng CSDL)
//   idN.MobileGate.Common/Models/RT_Common.cs:9-11 kế thừa WARTBase.cs:9-15
//   idN.MobileGate.Common/Models/c_K_DT_Sys.cs:9-17, c_K_DT_SysInfo.cs:8-24, c_K_DT_SysError.cs, c_K_DT_SysWarning.cs
//   idN.MobileGate.Constants/Error.DMS.Chain.cs:42 (TError.ErridNTVAN.WAS_Cm_GetDTime = "ErridN.MobileGate.WAS_Cm_GetDTime")
//
// GIỮ ĐÚNG NGUỒN & QUY ƯỚC:
//   * Route: POST /api/common/wa_common_getdtime (nhận [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]).
//   * Thời gian hệ thống: DateTime.UtcNow định dạng chuỗi "yyyy-MM-dd HH:mm:ss.ffffff" (đúng 26 ký tự).
//   * Đặt tại Data.c_K_DT_Sys.Lst_c_K_DT_SysInfo[0].Remark.
//   * Bỏ qua xác thực CheckAuthorize / CheckGatewayAuthentication theo quy ước chung của môi trường port.
//   * Ngoại lệ chưa bắt được PortExceptionFilter chuyển thành HTTP 400 có tiền tố đúng quy ước.
// ============================================================================================

/// <summary>
/// DTO yêu cầu cho action WA_Common_GetDTime — port 1:1 từ RQ_Common (CommonController.cs:24, 52-74).
/// </summary>
public class CommonGetDTimeReq
{
    public string? Tid { get; set; }                      // Transaction ID (CommonController.cs:68, 119)
    public string? OrgID { get; set; }                    // Mã tổ chức (CommonController.cs:73, 118)
    public string? NetworkID { get; set; }                // Mã mạng (CommonController.cs:72, 117)
    public string? AppVersion { get; set; }               // Phiên bản ứng dụng (CommonController.cs:74, 120)
    public string? WAUserCode { get; set; }               // Mã người dùng WA (CommonController.cs:53, 69)
    public int? PageIndex { get; set; }                   // Trang (CommonController.cs:63)
    public int? PageSize { get; set; }                    // Kích thước trang (CommonController.cs:64)
    public string? strRQ_Map_IDInBox { get; set; }        // Chuỗi JSON lồng nếu có (CommonController.cs:25, 54-61)
}

/// <summary>
/// Thông tin hệ thống chi tiết trong c_K_DT_Sys — port 1:1 từ c_K_DT_SysInfo.cs:8-24.
/// </summary>
public class c_K_DT_SysInfo
{
    public string? Tid { get; set; }
    public string? DigitalSignature { get; set; }
    public string? ErrorCode { get; set; } = "0";
    public string? FlagWarning { get; set; } = "0";
    public string? Remark { get; set; }
    public string? FlagCompress { get; set; }
    public string? FlagEncrypt { get; set; }
}

/// <summary>
/// Đối tượng lỗi hệ thống — port 1:1 từ c_K_DT_SysError.cs:7-14.
/// </summary>
public class c_K_DT_SysError
{
    public string? PCode { get; set; }
    public string? PVal { get; set; }
}

/// <summary>
/// Đối tượng cảnh báo hệ thống — port 1:1 từ c_K_DT_SysWarning.cs:7-14.
/// </summary>
public class c_K_DT_SysWarning
{
    public string? PCode { get; set; }
    public string? PVal { get; set; }
}

/// <summary>
/// Phong bì hệ thống c_K_DT_Sys — port 1:1 từ c_K_DT_Sys.cs:9-17.
/// </summary>
public class c_K_DT_Sys
{
    public List<c_K_DT_SysInfo> Lst_c_K_DT_SysInfo { get; set; } = new();
    public List<c_K_DT_SysError> Lst_c_K_DT_SysError { get; set; } = new();
    public List<c_K_DT_SysWarning> Lst_c_K_DT_SysWarning { get; set; } = new();
}

/// <summary>
/// Kết quả nghiệp vụ Common — port 1:1 từ RT_Common.cs:9-11 và WARTBase.cs:9-15.
/// </summary>
public class RT_Common
{
    public c_K_DT_Sys c_K_DT_Sys { get; set; } = new();
}

/// <summary>
/// DTO phản hồi trả về cho client — chứa Success và Data (RT_Common).
/// </summary>
public class CommonGetDTimeResp
{
    public bool Success { get; set; } = true;
    public RT_Common? Data { get; set; }
}

/// <summary>
/// Controller Port 1:1 cho CommonController (hạng mục #011).
/// </summary>
[ApiController]
[Route("api/common")]
public class CommonControllerPort : ControllerBase
{
    /// <summary>
    /// Port 1:1 action WA_Common_GetDTime (CommonController.cs:23-181).
    /// Nhận request body tuỳ chọn, trả thời gian UTC hệ thống dưới dạng chuỗi "yyyy-MM-dd HH:mm:ss.ffffff" trong Remark.
    /// </summary>
    [HttpPost("wa_common_getdtime")]
    public IActionResult WA_Common_GetDTime([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CommonGetDTimeReq? req)
    {

        // Xử lý strRQ_Map_IDInBox nếu có theo nguồn CommonController.cs:54-61
        // Nếu strRQ_Map_IDInBox là JSON không hợp lệ, Deserialize sẽ ném ngoại lệ -> PortExceptionFilter bắt và trả HTTP 400
        if (!string.IsNullOrEmpty(req?.strRQ_Map_IDInBox))
        {
            var objRQMap_IDInBox = JsonSerializer.Deserialize<CommonGetDTimeReq>(req.strRQ_Map_IDInBox);
            if (objRQMap_IDInBox != null)
            {
                req = objRQMap_IDInBox;
            }
        }

        // Nhận tham số header hoặc body theo nguồn CommonController.cs:72-74
        var headerOrgId = Request.Headers["orgId"].FirstOrDefault();
        var headerNetworkId = Request.Headers["networkId"].FirstOrDefault();
        var headerAppVer = Request.Headers["AppVer"].FirstOrDefault();

        var orgId = !string.IsNullOrEmpty(req?.OrgID) ? req.OrgID : headerOrgId;
        var networkId = !string.IsNullOrEmpty(req?.NetworkID) ? req.NetworkID : headerNetworkId;
        var appVersion = !string.IsNullOrEmpty(req?.AppVersion) ? req.AppVersion : headerAppVer;

        // Xác định Tid: dùng Tid từ client gửi hoặc tự sinh mã timestamp hợp lệ (CommonController.cs:68, 119)
        var tid = !string.IsNullOrWhiteSpace(req?.Tid)
            ? req.Tid
            : DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff");

        // Gọi hàm nghiệp vụ cốt lõi (tách hàm nghiệp vụ theo yêu cầu đề bài §P3)
        // Nguồn gọi: CommonController.cs:122-127 (_biz.WAS_Cm_GetDTime) -> Common.cs:1155 (Cm_GetDTime)
        var rtCommon = Cm_GetDTime(tid);

        // Trả kết quả thành công HTTP 200 (CommonController.cs:125, 142-147)
        return Ok(new CommonGetDTimeResp
        {
            Success = true,
            Data = rtCommon
        });
    }

    /// <summary>
    /// Hàm nghiệp vụ cốt lõi: sinh phong bì kết quả chứa thời gian UTC hiện tại đặt tại Remark.
    /// Nguồn: Common.cs:755-766 (Cm_GetDTime), gọi bởi Common.cs:1131-1186 (WAS_Cm_GetDTime).
    /// </summary>
    public static RT_Common Cm_GetDTime(string? strTid)
    {
        var tid = !string.IsNullOrWhiteSpace(strTid)
            ? strTid
            : DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff");

        // Lấy thời gian UTC hệ thống định dạng chuẩn yyyy-MM-dd HH:mm:ss.ffffff (Common.cs:761)
        var dtimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff");

        var objRT_Common = new RT_Common
        {
            c_K_DT_Sys = new c_K_DT_Sys
            {
                Lst_c_K_DT_SysInfo = new List<c_K_DT_SysInfo>
                {
                    new c_K_DT_SysInfo
                    {
                        Tid = tid,
                        DigitalSignature = null,
                        ErrorCode = "0",
                        FlagWarning = "0",
                        Remark = dtimeUtc,
                        FlagCompress = null,
                        FlagEncrypt = null
                    }
                },
                Lst_c_K_DT_SysError = new List<c_K_DT_SysError>(),
                Lst_c_K_DT_SysWarning = new List<c_K_DT_SysWarning>()
            }
        };

        return objRT_Common;
    }
}
