using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniGate.Data;
using MiniGate.Models.Port;

namespace MiniGate.Controllers.Port;

// ============================================================================================
// PORT 1:1 — InBrandController/WA_MstSv_Mst_Network_GetMinVersion (hạng mục #347) của dự án nguồn 2019.9.MobileGate.
//
// Nguồn controller (tầng gate):
//   InBrandController.cs:6055-6204   WA_MstSv_Mst_Network_GetMinVersion -> gọi _biz.WAS_MstSv_Mst_Network_GetMinVersion
// Nguồn biz:
//   InBrand.cs:15828-15924   WAS_MstSv_Mst_Network_GetMinVersion (biz wrapper, mã lỗi TError.ErridNTVAN.WAS_MstSv_Mst_Network_GetMinVersion)
//   InBrand.cs:16052-16169   MstSv_Mst_Network_GetMinVersion_Local (biz core: truy vấn select msmnw.MinVersion from MstSv_Mst_Network msmnw where msmnw.NetworkID = @strNetworkID)
// Nguồn model & hằng số:
//   idN.MobileGate.Common/Models/RQ_MstSv_Mst_Network.cs:9-19
//   idN.MobileGate.Common/Models/RT_MstSv_Mst_Network.cs:9-13 (kế thừa WARTBase)
//   idN.MobileGate.Common/Models/MstSv_Mst_Network.cs:9-36
//   idN.MobileGate.Constants/Error.idN.InBrand.cs:125, 128:
//     - TError.ErridNTVAN.MstSv_Mst_Network_GetMinVersion = "ErridInBrand.MstSv_Mst_Network_GetMinVersion"
//     - TError.ErridNTVAN.WAS_MstSv_Mst_Network_GetMinVersion = "ErridInBrand.WAS_MstSv_Mst_Network_GetMinVersion"
//
// GIỮ ĐÚNG NGUỒN & QUY ƯỚC:
//   * Route: POST /api/inbrand/wa_mstsv_mst_network_getminversion (nhận [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]).
//   * Lấy MinVersion từ bảng MstSv_Mst_Network theo NetworkID (từ header networkId hoặc body NetworkID).
//   * Đặt MinVersion tại Data.c_K_DT_Sys.Lst_c_K_DT_SysInfo[0].Remark (khớp với CmUtils.CMyDataSet.SetRemark tại InBrand.cs:16128).
//   * Bỏ qua xác thực CheckAuthorize / CheckGatewayAuthentication theo quy ước chung của môi trường port (§P3, PORT_RULES_MiniGate.md §1 & §2.5).
//   * Ngoại lệ trả về mã lỗi MstSv_Mst_Network_GetMinVersion (InBrand.cs:16069, 16118) qua PortResult.Fail.
// ============================================================================================

/// <summary>
/// DTO yêu cầu cho action WA_MstSv_Mst_Network_GetMinVersion — port 1:1 từ RQ_MstSv_Mst_Network (InBrandController.cs:6055, RQ_MstSv_Mst_Network.cs:9-19).
/// </summary>
public class InBrandMstSvMstNetworkGetMinVersionReq
{
    public string? Tid { get; set; }                      // Transaction ID (InBrandController.cs:6058, 6089)
    public string? OrgID { get; set; }                    // Mã tổ chức (InBrandController.cs:6081, 6145)
    public string? NetworkID { get; set; }                // Mã mạng (InBrandController.cs:6080, 6144)
    public string? AppVersion { get; set; }               // Phiên bản ứng dụng (InBrandController.cs:6082)
    public string? AccessToken { get; set; }              // Token truy cập OAuth2 (InBrandController.cs:6074-6079, 6090)
    public string? GwUserCode { get; set; }               // Người dùng Gateway (InBrandController.cs:6086)
    public string? GwPassword { get; set; }               // Mật khẩu Gateway (InBrandController.cs:6087)
    public string? WAUserCode { get; set; }               // Người dùng WA (InBrand.cs:15867)
    public string? WAUserPassword { get; set; }           // Mật khẩu người dùng WA (InBrand.cs:15868)
    public int? pageIndex { get; set; }                   // Trang bắt đầu (InBrandController.cs:6091)
    public int? pageSize { get; set; }                    // Kích thước trang (InBrandController.cs:6092)
    public string? Rt_Cols_MstSv_Mst_Network { get; set; } // Danh sách cột trả về (RQ_MstSv_Mst_Network.cs:11, InBrand.cs:15858)
}

/// <summary>
/// DTO phản hồi trả về cho client — chứa Success và Data (RT_MstSv_Mst_Network).
/// </summary>
public class InBrandMstSvMstNetworkGetMinVersionResp
{
    public bool Success { get; set; } = true;
    public RT_MstSv_Mst_Network? Data { get; set; }
}

/// <summary>
/// Controller Port 1:1 cho InBrandController (hạng mục #347).
/// </summary>
[ApiController]
[Route("api/inbrand")]
public class InBrandControllerPort : ControllerBase
{
    /// <summary>
    /// Port 1:1 action WA_MstSv_Mst_Network_GetMinVersion (InBrandController.cs:6055-6204).
    /// Lấy phiên bản tối thiểu MinVersion của NetworkID từ bảng MstSv_Mst_Network, trả về trong Remark của c_K_DT_Sys.
    /// </summary>
    [HttpPost("wa_mstsv_mst_network_getminversion")]
    public async Task<IActionResult> WA_MstSv_Mst_Network_GetMinVersion(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InBrandMstSvMstNetworkGetMinVersionReq? req,
        [FromServices] AppDbContext db)
    {
        // 1. Nhận tham số header theo nguồn InBrandController.cs:6074-6082
        var headerNetworkId = Request?.Headers["networkId"].FirstOrDefault();
        var headerOrgId = Request?.Headers["orgId"].FirstOrDefault();
        var headerAppVer = Request?.Headers["AppVer"].FirstOrDefault();
        var headerAuth = Request?.Headers["Authorization"].FirstOrDefault();

        // Ưu tiên NetworkID truyền từ body hoặc header networkId (InBrandController.cs:6080, 6144)
        var networkId = !string.IsNullOrEmpty(req?.NetworkID) ? req.NetworkID : headerNetworkId;

        // 2. Bỏ qua CheckAuthorize và CheckGatewayAuthentication (ghi GAP theo PORT_RULES_MiniGate.md §1 & §2.5)

        // 3. Xác định Tid: dùng Tid từ client gửi hoặc tự sinh mã timestamp hợp lệ (InBrandController.cs:6089)
        var tid = !string.IsNullOrWhiteSpace(req?.Tid)
            ? req.Tid
            : DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff");

        // 4. Gọi nghiệp vụ cốt lõi theo nguồn InBrand.cs:16052-16169 (MstSv_Mst_Network_GetMinVersion_Local)
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // Truy vấn SQL nguồn InBrand.cs:16097-16104:
            // select msmnw.MinVersion from MstSv_Mst_Network msmnw where msmnw.NetworkID = @strNetworkID;
            string? strMinVersion = null;
            if (!string.IsNullOrEmpty(networkId))
            {
                var networkIdPattern = PortQuery.Equal(networkId);
                strMinVersion = await db.Set<MstSv_Mst_Network>()
                    .Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), networkIdPattern, PortQuery.Esc))
                    .Select(x => x.MinVersion)
                    .FirstOrDefaultAsync();
            }

            await tx.CommitAsync();

            // 5. Kết quả thành công theo nguồn InBrand.cs:16128 và InBrandController.cs:6166-6169:
            // CmUtils.CMyDataSet.SetRemark(ref mdsFinal, strMinVersion);
            // objRT_MstSv_Mst_Network.c_K_DT_Sys = TUtils.CMyDataList.CProcessMyDS(mdsReturn);
            // resultEntry.Data = objRT_MstSv_Mst_Network;
            var rtResult = new RT_MstSv_Mst_Network
            {
                Lst_MstSv_Mst_Network = null,
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
                            Remark = strMinVersion,
                            FlagCompress = null,
                            FlagEncrypt = null
                        }
                    },
                    Lst_c_K_DT_SysError = new List<c_K_DT_SysError>(),
                    Lst_c_K_DT_SysWarning = new List<c_K_DT_SysWarning>()
                }
            };

            return Ok(new InBrandMstSvMstNetworkGetMinVersionResp
            {
                Success = true,
                Data = rtResult
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Nguồn: InBrand.cs:16069, 16118 (strErrorCodeDefault = TError.ErridNTVAN.MstSv_Mst_Network_GetMinVersion)
            return PortResult.Fail("MstSv_Mst_Network_GetMinVersion", "Exception", ex.Message);
        }
    }
}
