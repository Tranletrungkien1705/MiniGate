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
// PORT 1:1 — QRBrandController của dự án nguồn 2019.9.MobileGate.
//
// 1. WA_MstSv_Mst_Network_GetMinVersion (hạng mục #108):
//   - Nguồn controller (tầng gate):
//     QRBrandController.cs:4795-4944       WA_MstSv_Mst_Network_GetMinVersion -> gọi _biz.QRBrand_WAS_MstSv_Mst_Network_GetMinVersion
//   - Nguồn biz:
//     QRBrand.cs:9203-9298                 QRBrand_WAS_MstSv_Mst_Network_GetMinVersion (biz wrapper, mã lỗi TError.ErridNTVAN.WAS_MstSv_Mst_Network_GetMinVersion)
//     QRBrand.cs:9300-9421                 QRBrand_MstSv_Mst_Network_GetMinVersion (biz core: truy vấn select msmnw.MinVersion from MstSv_Mst_Network msmnw where msmnw.NetworkID = @strNetworkID)
//   - Nguồn model & hằng số:
//     idN.MobileGate.Common/Models/RQ_MstSv_Mst_Network.cs:9-19
//     idN.MobileGate.Common/Models/RT_MstSv_Mst_Network.cs:9-13 (kế thừa WARTBase)
//     idN.MobileGate.Common/Models/MstSv_Mst_Network.cs:9-36
//     idN.MobileGate.Constants/Error.idN.InBrand.cs:125, 128:
//       - TError.ErridNTVAN.MstSv_Mst_Network_GetMinVersion = "ErridInBrand.MstSv_Mst_Network_GetMinVersion"
//       - TError.ErridNTVAN.WAS_MstSv_Mst_Network_GetMinVersion = "ErridInBrand.WAS_MstSv_Mst_Network_GetMinVersion"
//
// 2. WA_ELTS_eTEM_Search (hạng mục #742):
//   - Nguồn controller (tầng gate):
//     QRBrandController.cs:14133-14282     WA_ELTS_eTEM_Search -> gọi _biz.WAS_ELTS_eTEM_Search_New20210813
//   - Nguồn biz:
//     ElasticSearch.cs:2168-2306           WAS_ELTS_eTEM_Search_New20210813 (mã lỗi mặc định TError.ErridNTVAN.WAS_ELTS_eTEM_Search)
//     ElasticSearch.cs:1436-1723           ELTS_eTEM_Search (mã lỗi mặc định TError.ErridNTVAN.ELTS_eTEM_Search)
//   - Nguồn model & hằng số:
//     RQ_ELTS_eTEM_Search / RT_ELTS_eTEM_Search: KHÔNG có định nghĩa trong source_excerpt.txt
//     TError.ErridNTVAN.CmSys_InvalidOutSite (ElasticSearch.cs:1666)
//   - Dùng lại DTO/entity đã có: RT_ELTS_eTEM_Search, InBrandEltsEtemSearchReq (cùng RQ_ELTS_eTEM_Search),
//     Mst_DomainConfig (Models/Port/Mst_DomainConfig.cs, #405).
//
// 3. WA_Mst_NotifyUpdVersion_Get (hạng mục #750):
//   - Nguồn controller (tầng gate):
//     QRBrandController.cs:15409-15551     WA_Mst_NotifyUpdVersion_Get -> gọi _biz.WAS_QRBrand_Mst_NotifyUpdVersion_Get
//   - Nguồn biz:
//     QRBrand.cs:17191-17277               WAS_QRBrand_Mst_NotifyUpdVersion_Get (biz wrapper, mã lỗi TError.ErridNTVAN.WAS_Mst_NotifyUpdVersion_Get)
//     QRBrand.cs:17279-17412               QRBrand_Mst_NotifyUpdVersion_Get (biz core: inline SQL kiểm tra MinVersion/DefaultVersion và lấy nội dung từ Mst_NotifyUpdVersion)
//   - Nguồn model & hằng số:
//     idN.MobileGate.Common/Models/RQ_Mst_NotifyUpdVersion.cs:9-19
//     idN.MobileGate.Common/Models/RT_Mst_NotifyUpdVersion.cs:9-13
//     idN.MobileGate.Common/Models/InBrand/Mst_NotifyUpdVersion.cs:9-24
//     idN.MobileGate.Constants/Error.idN.InBrand.cs:
//       - TError.ErridNTVAN.Mst_NotifyUpdVersion_Get = "ErridInBrand.Mst_NotifyUpdVersion_Get"
//       - TError.ErridNTVAN.WAS_Mst_NotifyUpdVersion_Get = "ErridInBrand.WAS_Mst_NotifyUpdVersion_Get"
//
// GIỮ ĐÚNG NGUỒN & QUY ƯỚC:
//   * Route: POST /api/qrbrand/<action> (nhận [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]).
//   * Dùng lại các kiểu thực thể và DTO đã có (RT_MstSv_Mst_Network, RT_Mst_NotifyUpdVersion, c_K_DT_Sys...).
//   * Bỏ qua xác thực CheckAuthorize / CheckGatewayAuthentication theo quy ước chung của môi trường port (§P3, PORT_RULES_MiniGate.md §1 & §2.5).
//   * Ngoại lệ trả về mã lỗi qua PortResult.Fail.
// ============================================================================================

/// <summary>
/// DTO yêu cầu cho action WA_MstSv_Mst_Network_GetMinVersion — port 1:1 từ RQ_MstSv_Mst_Network (QRBrandController.cs:6326, RQ_MstSv_Mst_Network.cs:9-19).
/// </summary>
public class QRBrandMstSvMstNetworkGetMinVersionReq
{
    public string? Tid { get; set; }                      // Transaction ID (QRBrandController.cs:6329, 6360)
    public string? OrgID { get; set; }                    // Mã tổ chức (QRBrandController.cs:6352, 6416)
    public string? NetworkID { get; set; }                // Mã mạng (QRBrandController.cs:6351, 6415)
    public string? AppVersion { get; set; }               // Phiên bản ứng dụng (QRBrandController.cs:6353)
    public string? AccessToken { get; set; }              // Token truy cập OAuth2 (QRBrandController.cs:6346-6350, 6361)
    public string? GwUserCode { get; set; }               // Người dùng Gateway (QRBrandController.cs:6357)
    public string? GwPassword { get; set; }               // Mật khẩu Gateway (QRBrandController.cs:6358)
    public string? WAUserCode { get; set; }               // Người dùng WA (QRBrand.cs:13648)
    public string? WAUserPassword { get; set; }           // Mật khẩu người dùng WA (QRBrand.cs:13649)
    public int? pageIndex { get; set; }                   // Trang bắt đầu (QRBrandController.cs:6362)
    public int? pageSize { get; set; }                    // Kích thước trang (QRBrandController.cs:6363)
    public string? Rt_Cols_MstSv_Mst_Network { get; set; } // Danh sách cột trả về (RQ_MstSv_Mst_Network.cs:11, QRBrand.cs:13640)
}
/// <summary>
/// DTO phản hồi trả về cho client — chứa Success và Data (RT_MstSv_Mst_Network).
/// </summary>
public class QRBrandMstSvMstNetworkGetMinVersionResp
{
    public bool Success { get; set; } = true;
    public RT_MstSv_Mst_Network? Data { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action WA_Mst_NotifyUpdVersion_Get — port 1:1 từ RQ_Mst_NotifyUpdVersion (QRBrandController.cs:15409, RQ_Mst_NotifyUpdVersion.cs:9-19).
/// </summary>
public class QRBrandMstNotifyUpdVersionGetReq
{
    public string? Tid { get; set; }                      // Transaction ID (QRBrandController.cs:15413, 15467)
    public string? OrgID { get; set; }                    // Mã tổ chức (QRBrandController.cs:15437, 15486)
    public string? NetworkID { get; set; }                // Mã mạng (QRBrandController.cs:15436, 15485)
    public string? AppVersion { get; set; }               // Phiên bản ứng dụng (QRBrandController.cs:15438, 15487)
    public string? AccessToken { get; set; }              // Token truy cập OAuth2 (QRBrandController.cs:15431-15435, 15446)
    public string? GwUserCode { get; set; }               // Người dùng Gateway (QRBrandController.cs:15443)
    public string? GwPassword { get; set; }               // Mật khẩu Gateway (QRBrandController.cs:15444)
    public string? WAUserCode { get; set; }               // Người dùng WA (QRBrand.cs:17229)
    public string? WAUserPassword { get; set; }           // Mật khẩu người dùng WA (QRBrand.cs:17230)
    public string? Rt_Cols_Mst_NotifyUpdVersion { get; set; } // Danh sách cột trả về (RQ_Mst_NotifyUpdVersion.cs:11, QRBrand.cs:17221)
}

/// <summary>
/// DTO phản hồi trả về cho client cho WA_Mst_NotifyUpdVersion_Get — chứa Success và Data (RT_Mst_NotifyUpdVersion).
/// </summary>
public class QRBrandMstNotifyUpdVersionGetResp
{
    public bool Success { get; set; } = true;
    public RT_Mst_NotifyUpdVersion? Data { get; set; }
}

/// <summary>
/// DTO phản hồi trả về cho client cho WA_ELTS_eTEM_Search — chứa Success và Data (RT_ELTS_eTEM_Search).
/// </summary>
public class QRBrandEltsEtemSearchResp
{
    public bool Success { get; set; } = true;
    public RT_ELTS_eTEM_Search? Data { get; set; }
}

/// <summary>
/// Controller Port 1:1 cho QRBrandController (hạng mục #750, #108 và #742).
/// </summary>
[ApiController]
[Route("api/qrbrand")]
public class QRBrandControllerPort : ControllerBase
{
    /// <summary>
    /// Port 1:1 action WA_MstSv_Mst_Network_GetMinVersion (QRBrandController.cs:4795-4944).
    /// Lấy phiên bản tối thiểu MinVersion của NetworkID từ bảng MstSv_Mst_Network, trả về trong Remark của c_K_DT_Sys.
    /// </summary>
    [HttpPost("wa_mstsv_mst_network_getminversion")]
    public async Task<IActionResult> WA_MstSv_Mst_Network_GetMinVersion(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] QRBrandMstSvMstNetworkGetMinVersionReq? req,
        [FromServices] AppDbContext db)
    {
        // 1. Nhận tham số header theo nguồn QRBrandController.cs:6345-6353
        var headerNetworkId = Request?.Headers["networkId"].FirstOrDefault();
        var headerOrgId = Request?.Headers["orgId"].FirstOrDefault();
        var headerAppVer = Request?.Headers["AppVer"].FirstOrDefault();
        var headerAuth = Request?.Headers["Authorization"].FirstOrDefault();

        // Ưu tiên NetworkID truyền từ body hoặc header networkId (QRBrandController.cs:6351, 6415)
        var networkId = !string.IsNullOrEmpty(req?.NetworkID) ? req.NetworkID : headerNetworkId;

        // 2. Bỏ qua CheckAuthorize và CheckGatewayAuthentication (ghi GAP theo PORT_RULES_MiniGate.md §1 & §2.5)

        // 3. Xác định Tid: dùng Tid từ client gửi hoặc tự sinh mã timestamp hợp lệ (QRBrandController.cs:6360)
        var tid = !string.IsNullOrWhiteSpace(req?.Tid)
            ? req.Tid
            : DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff");

        // 4. Gọi nghiệp vụ cốt lõi theo nguồn QRBrand.cs:9300-9421 (QRBrand_MstSv_Mst_Network_GetMinVersion)
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // Truy vấn SQL nguồn QRBrand.cs:13758-13766:
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

            // 5. Kết quả thành công theo nguồn QRBrand.cs:13786 và QRBrandController.cs:6436-6440:
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

            return Ok(new QRBrandMstSvMstNetworkGetMinVersionResp
            {
                Success = true,
                Data = rtResult
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Nguồn: QRBrand.cs:13726, 13805 (strErrorCodeDefault = TError.ErridNTVAN.MstSv_Mst_Network_GetMinVersion)
            return PortResult.Fail("MstSv_Mst_Network_GetMinVersion", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action WA_ELTS_eTEM_Search (QRBrandController.cs:14133-14282).
    /// Kiểm tra tên miền của URI trong Mst_DomainConfig; URI không phải URL -> ERR_IU, tên miền không cấu hình -> ERR_AUS.
    /// Nhánh hợp lệ gọi dịch vụ ngoài ElasticSearch (GAP — không mô phỏng).
    /// </summary>
    [HttpPost("wa_elts_etem_search")]
    public async Task<IActionResult> WA_ELTS_eTEM_Search(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InBrandEltsEtemSearchReq? req,
        [FromServices] AppDbContext db)
    {
        // 1. Tid: gate gán GetNextTId() (QRBrandController.cs:14199) — ở đích dùng Tid client gửi, thiếu thì sinh theo mốc thời gian.
        var tid = !string.IsNullOrWhiteSpace(req?.Tid)
            ? req.Tid
            : DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff");

        // 2. Bỏ qua CheckAuthorize / CheckGatewayAuthentication (GAP theo PORT_RULES_MiniGate §1).

        var rt = new RT_ELTS_eTEM_Search();
        var uri = req?.URI;

        // 3. Refine and Check Input (ElasticSearch.cs:2203-2250)
        //    Nguồn: strUri.Contains("http://") || strUri.Contains("https://") — URI null sẽ ném NullReference ở nguồn,
        //    ở đích để PortExceptionFilter trả 400 (không tự đặt giá trị thay thế).
        var isUrl = uri!.Contains("http://") || uri.Contains("https://");
        if (isUrl)
        {
            // ElasticSearch.cs:2212-2213: host của URI
            var host = new Uri(uri).Host;

            // ElasticSearch.cs:2215-2228: select t.* from Mst_DomainConfig t where (1=1) and t.FlagActive = '1' and t.DomainValues = @strDomainValues
            var hostPattern = PortQuery.Equal(host);
            var domainFound = await db.Set<Mst_DomainConfig>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), "1", PortQuery.Esc)
                            && EF.Functions.Like((x.DomainValues ?? "").ToLower(), hostPattern, PortQuery.Esc))
                .AnyAsync();

            if (!domainFound)
            {
                // ElasticSearch.cs:2236-2239
                rt.ELTSBizCodeReturn = "ERR_AUS";
                rt.TrackingCode = uri;
                rt.RefInfo = uri;
            }
        }
        else
        {
            // ElasticSearch.cs:2244-2247
            rt.ELTSBizCodeReturn = "ERR_IU";
            rt.TrackingCode = uri;
            rt.RefInfo = uri;
        }

        // 4. ElasticSearch.cs:2252-2276: nhánh hợp lệ gọi OS_MstSv_ElasticSearchServices.Instance.WA_ELTS_eTEM_Search
        //    (dịch vụ ngoài) -> GAP: KHÔNG mô phỏng, các trường kết quả của nhánh này để null.

        // 5. Phong bì kết quả (QRBrandController.cs:14234: resultEntry.Data = objRT_ELTS_eTEM_Search)
        rt.c_K_DT_Sys = new c_K_DT_Sys
        {
            Lst_c_K_DT_SysInfo = new List<c_K_DT_SysInfo>
            {
                new c_K_DT_SysInfo
                {
                    Tid = tid,
                    DigitalSignature = null,
                    ErrorCode = "0",
                    FlagWarning = "0",
                    Remark = null,
                    FlagCompress = null,
                    FlagEncrypt = null
                }
            },
            Lst_c_K_DT_SysError = new List<c_K_DT_SysError>(),
            Lst_c_K_DT_SysWarning = new List<c_K_DT_SysWarning>()
        };

        return Ok(new QRBrandEltsEtemSearchResp
        {
            Success = true,
            Data = rt
        });
    }

    /// <summary>
    /// Port 1:1 action WA_Mst_NotifyUpdVersion_Get (QRBrandController.cs:15409-15551).
    /// Kiểm tra và lấy thông tin thông báo cập nhật phiên bản ứng dụng theo NetworkID và AppVersion.
    /// </summary>
    [HttpPost("wa_mst_notifyupdversion_get")]
    public async Task<IActionResult> WA_Mst_NotifyUpdVersion_Get(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] QRBrandMstNotifyUpdVersionGetReq? req,
        [FromServices] AppDbContext db)
    {
        // 1. Nhận tham số header theo nguồn QRBrandController.cs:15431-15438
        var headerNetworkId = Request?.Headers["networkId"].FirstOrDefault();
        var headerOrgId = Request?.Headers["orgId"].FirstOrDefault();
        var headerAppVer = Request?.Headers["AppVer"].FirstOrDefault();
        var headerAuth = Request?.Headers["Authorization"].FirstOrDefault();

        // Ưu tiên NetworkID / AppVersion truyền từ body hoặc header (QRBrandController.cs:15436-15438, 15485-15487)
        var networkId = !string.IsNullOrEmpty(headerNetworkId) ? headerNetworkId : req?.NetworkID;
        var appVersion = !string.IsNullOrEmpty(headerAppVer) ? headerAppVer : req?.AppVersion;
        var orgId = !string.IsNullOrEmpty(headerOrgId) ? headerOrgId : req?.OrgID;

        // 2. Bỏ qua CheckAuthorize và CheckGatewayAuthentication (ghi GAP theo PORT_RULES_MiniGate.md §1 & §2.5)

        // 3. Xác định Tid: dùng Tid từ client gửi hoặc tự sinh mã timestamp hợp lệ (QRBrandController.cs:15413, 15467)
        var tid = !string.IsNullOrWhiteSpace(req?.Tid)
            ? req.Tid
            : DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff");

        // 4. Mở transaction và thực hiện nghiệp vụ theo QRBrand.cs:17279-17412 (QRBrand_Mst_NotifyUpdVersion_Get)
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var networkList = new List<Mst_NotifyUpdVersion>();

            if (!string.IsNullOrEmpty(networkId))
            {
                var networkIdPattern = PortQuery.Equal(networkId);
                var network = await db.Set<MstSv_Mst_Network>()
                    .Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), networkIdPattern, PortQuery.Esc))
                    .Select(x => new { x.NetworkID, x.DefaultVersion, x.MinVersion })
                    .FirstOrDefaultAsync();

                if (network != null)
                {
                    var curAppVer = appVersion ?? "";
                    var defVer = network.DefaultVersion;
                    var minVer = network.MinVersion;

                    // So sánh theo quy tắc SQL Server (@strAppVersion < msmnw.DefaultVersion / MinVersion)
                    // (QRBrand.cs:17341-17348)
                    var flagNewVersion = (!string.IsNullOrEmpty(defVer) && string.Compare(curAppVer, defVer, StringComparison.Ordinal) < 0) ? "1" : "0";
                    var flagForcedUpdate = (!string.IsNullOrEmpty(minVer) && string.Compare(curAppVer, minVer, StringComparison.Ordinal) < 0) ? "1" : "0";

                    string? notifyNo = null;
                    if (!string.IsNullOrEmpty(minVer) && !string.IsNullOrEmpty(defVer) &&
                        string.Compare(curAppVer, minVer, StringComparison.Ordinal) < 0 &&
                        string.Compare(curAppVer, defVer, StringComparison.Ordinal) < 0)
                    {
                        notifyNo = "FORCEDUPD";
                    }
                    else if (!string.IsNullOrEmpty(minVer) && !string.IsNullOrEmpty(defVer) &&
                        string.Compare(curAppVer, minVer, StringComparison.Ordinal) >= 0 &&
                        string.Compare(curAppVer, defVer, StringComparison.Ordinal) < 0)
                    {
                        notifyNo = "OPTIONALUPD";
                    }

                    var flagStrongForcedUpdate = (notifyNo == "FORCEDUPD") ? "1" : "0";

                    // Left join Mst_NotifyUpdVersion f on t.NotifyNo = f.NotifyNo (QRBrand.cs:17357)
                    string? notifyContent = null;
                    string? linkUpdUrl = null;

                    if (!string.IsNullOrEmpty(notifyNo))
                    {
                        var notifyPattern = PortQuery.Equal(notifyNo);
                        var notify = await db.Set<Mst_NotifyUpdVersion>()
                            .Where(x => EF.Functions.Like((x.NotifyNo ?? "").ToLower(), notifyPattern, PortQuery.Esc))
                            .Select(x => new { x.NotifyContent, x.LinkUpdUrl })
                            .FirstOrDefaultAsync();

                        if (notify != null)
                        {
                            notifyContent = notify.NotifyContent;
                            linkUpdUrl = notify.LinkUpdUrl;
                        }
                    }

                    networkList.Add(new Mst_NotifyUpdVersion
                    {
                        FlagNewVersion = flagNewVersion,
                        FlagForcedUpdate = flagForcedUpdate,
                        NotifyContent = notifyContent,
                        LinkUpdUrl = linkUpdUrl,
                        FlagStrongForcedUpdate = flagStrongForcedUpdate
                    });
                }
            }

            await tx.CommitAsync();

            // 5. Kết quả thành công theo nguồn QRBrand.cs:17244-17246 và QRBrandController.cs:15507
            var rtResult = new RT_Mst_NotifyUpdVersion
            {
                Lst_Mst_NotifyUpdVersion = networkList,
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
                            Remark = null,
                            FlagCompress = null,
                            FlagEncrypt = null
                        }
                    },
                    Lst_c_K_DT_SysError = new List<c_K_DT_SysError>(),
                    Lst_c_K_DT_SysWarning = new List<c_K_DT_SysWarning>()
                }
            };

            return Ok(new QRBrandMstNotifyUpdVersionGetResp
            {
                Success = true,
                Data = rtResult
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Nguồn: QRBrand.cs:17204, 17298 (strErrorCodeDefault = TError.ErridNTVAN.Mst_NotifyUpdVersion_Get)
            return PortResult.Fail("Mst_NotifyUpdVersion_Get", "Exception", ex.Message);
        }
    }
}
