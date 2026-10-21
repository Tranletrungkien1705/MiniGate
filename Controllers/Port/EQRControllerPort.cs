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
// PORT 1:1 — EQRController các hạng mục của dự án nguồn 2019.9.MobileGate:
//   1. WA_MstSv_Mst_Network_GetMinVersion (hạng mục #108):
//      - Nguồn controller: EQRController.cs:6326-6475
//      - Nguồn biz: EQR.cs:13610-13705 (WAS_), EQR.cs:13707-13828 (core)
//   2. WA_Mst_NotifyUpdVersion_Get (hạng mục #174):
//      - Nguồn controller: EQRController.cs:17373-17515
//      - Nguồn biz: EQR.cs:21864-21950 (EQR_WAS_Mst_NotifyUpdVersion_Get), EQR.cs:21952-22090 (EQR_Mst_NotifyUpdVersion_Get)
//      - Mã lỗi: TError.ErridNTVAN.Mst_NotifyUpdVersion_Get = "ErridInBrand.Mst_NotifyUpdVersion_Get"
//   3. WA_ELTS_eTEM_Search (hạng mục #166):
//      - Nguồn controller: EQRController.cs:16097-16246
//      - Nguồn biz: ElasticSearch.cs:2168-2306 (WAS_ELTS_eTEM_Search_New20210813), ElasticSearch.cs:1436-1723 (ELTS_eTEM_Search)
//      - Mã lỗi: TError.ErridNTVAN.WAS_ELTS_eTEM_Search, TError.ErridNTVAN.ELTS_eTEM_Search, TError.ErridNTVAN.CmSys_InvalidOutSite
// ============================================================================================

/// <summary>
/// DTO yêu cầu cho action WA_MstSv_Mst_Network_GetMinVersion — port 1:1 từ RQ_MstSv_Mst_Network (EQRController.cs:6326, RQ_MstSv_Mst_Network.cs:9-19).
/// </summary>
public class EQRMstSvMstNetworkGetMinVersionReq
{
    public string? Tid { get; set; }                      // Transaction ID (EQRController.cs:6329, 6360)
    public string? OrgID { get; set; }                    // Mã tổ chức (EQRController.cs:6352, 6416)
    public string? NetworkID { get; set; }                // Mã mạng (EQRController.cs:6351, 6415)
    public string? AppVersion { get; set; }               // Phiên bản ứng dụng (EQRController.cs:6353)
    public string? AccessToken { get; set; }              // Token truy cập OAuth2 (EQRController.cs:6346-6350, 6361)
    public string? GwUserCode { get; set; }               // Người dùng Gateway (EQRController.cs:6357)
    public string? GwPassword { get; set; }               // Mật khẩu Gateway (EQRController.cs:6358)
    public string? WAUserCode { get; set; }               // Người dùng WA (EQR.cs:13648)
    public string? WAUserPassword { get; set; }           // Mật khẩu người dùng WA (EQR.cs:13649)
    public int? pageIndex { get; set; }                   // Trang bắt đầu (EQRController.cs:6362)
    public int? pageSize { get; set; }                    // Kích thước trang (EQRController.cs:6363)
    public string? Rt_Cols_MstSv_Mst_Network { get; set; } // Danh sách cột trả về (RQ_MstSv_Mst_Network.cs:11, EQR.cs:13640)
}

/// <summary>
/// Kết quả nghiệp vụ trả về — port 1:1 từ RT_MstSv_Mst_Network.cs:9-13 và WARTBase.cs:9-15.
/// </summary>
public class RT_MstSv_Mst_Network
{
    public List<MstSv_Mst_Network>? Lst_MstSv_Mst_Network { get; set; }
    public c_K_DT_Sys c_K_DT_Sys { get; set; } = new();
}

/// <summary>
/// DTO phản hồi trả về cho client — chứa Success và Data (RT_MstSv_Mst_Network).
/// </summary>
public class EQRMstSvMstNetworkGetMinVersionResp
{
    public bool Success { get; set; } = true;
    public RT_MstSv_Mst_Network? Data { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action WA_Mst_NotifyUpdVersion_Get — port 1:1 từ RQ_Mst_NotifyUpdVersion (EQRController.cs:17373, RQ_Mst_NotifyUpdVersion.cs:9-19).
/// </summary>
public class EQRMstNotifyUpdVersionGetReq
{
    public string? Tid { get; set; }                      // Transaction ID (EQRController.cs:17377, 17431)
    public string? OrgID { get; set; }                    // Mã tổ chức (EQRController.cs:17401, 17450)
    public string? NetworkID { get; set; }                // Mã mạng (EQRController.cs:17400, 17449)
    public string? AppVersion { get; set; }               // Phiên bản ứng dụng (EQRController.cs:17402, 17451)
    public string? AccessToken { get; set; }              // Token truy cập OAuth2 (EQRController.cs:17395-17398, 17410)
    public string? GwUserCode { get; set; }               // Người dùng Gateway (EQRController.cs:17407)
    public string? GwPassword { get; set; }               // Mật khẩu Gateway (EQRController.cs:17408)
    public string? WAUserCode { get; set; }               // Người dùng WA (EQR.cs:21902)
    public string? WAUserPassword { get; set; }           // Mật khẩu người dùng WA (EQR.cs:21903)
    public string? Rt_Cols_Mst_NotifyUpdVersion { get; set; } // Danh sách cột trả về (RQ_Mst_NotifyUpdVersion.cs:11, EQR.cs:21894)
}

/// <summary>
/// Kết quả nghiệp vụ trả về cho WA_Mst_NotifyUpdVersion_Get — port 1:1 từ RT_Mst_NotifyUpdVersion.cs:9-13 và WARTBase.cs:9-15.
/// </summary>
public class RT_Mst_NotifyUpdVersion
{
    public List<Mst_NotifyUpdVersion>? Lst_Mst_NotifyUpdVersion { get; set; }
    public c_K_DT_Sys c_K_DT_Sys { get; set; } = new();
}

/// <summary>
/// DTO phản hồi trả về cho client cho WA_Mst_NotifyUpdVersion_Get — chứa Success và Data (RT_Mst_NotifyUpdVersion).
/// </summary>
public class EQRMstNotifyUpdVersionGetResp
{
    public bool Success { get; set; } = true;
    public RT_Mst_NotifyUpdVersion? Data { get; set; }
}

// ============================================================================================
// PORT 1:1 — EQRController/WA_ELTS_eTEM_Search (hạng mục #166) của dự án nguồn 2019.9.MobileGate.
//
// Nguồn controller (tầng gate):
//   EQRController.cs:16097-16246  WA_ELTS_eTEM_Search -> gọi _biz.WAS_ELTS_eTEM_Search_New20210813
// Nguồn biz:
//   ElasticSearch.cs:2168-2306  WAS_ELTS_eTEM_Search_New20210813 (mã lỗi mặc định TError.ErridNTVAN.WAS_ELTS_eTEM_Search)
//   ElasticSearch.cs:1436-1723  ELTS_eTEM_Search (mã lỗi mặc định TError.ErridNTVAN.ELTS_eTEM_Search)
// Nguồn model & hằng số:
//   RQ_ELTS_eTEM_Search / RT_ELTS_eTEM_Search: KHÔNG có định nghĩa trong source_excerpt.txt
//   TError.ErridNTVAN.CmSys_InvalidOutSite (ElasticSearch.cs:1666)
//
// GIỮ ĐÚNG NGUỒN & QUY ƯỚC:
//   * Route: POST /api/eqr/wa_elts_etem_search (nhận [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]).
//   * Bước 1 (ElasticSearch.cs:2203-2250): URI chứa http:// hoặc https:// -> tra Mst_DomainConfig
//     (FlagActive = '1' and DomainValues = host); KHÔNG thấy -> ELTSBizCodeReturn = "ERR_AUS".
//     URI không phải URL -> ELTSBizCodeReturn = "ERR_IU". Cả hai nhánh đặt TrackingCode = RefInfo = URI.
//   * Bước 2 (ElasticSearch.cs:2252-2276): chỉ khi bước 1 KHÔNG lỗi mới gọi dịch vụ ngoài
//     OS_MstSv_ElasticSearchServices.Instance.WA_ELTS_eTEM_Search -> GAP, KHÔNG mô phỏng.
//   * Bỏ qua CheckAuthorize / CheckGatewayAuthentication / ProcessBizReq_OutSide theo quy ước chung của môi trường port.
// ============================================================================================

/// <summary>
/// DTO yêu cầu cho action WA_ELTS_eTEM_Search — port 1:1 từ RQ_ELTS_eTEM_Search
/// (EQRController.cs:16097, ElasticSearch.cs:2255-2275). Định nghĩa model nguồn không có trong excerpt nên
/// chỉ khai các trường mà gate/biz THỰC SỰ đọc.
/// </summary>
public class EQREltsEtemSearchReq
{
    public string? Tid { get; set; }                      // Transaction ID (EQRController.cs:16100, 16163)
    public string? URI { get; set; }                      // Chuỗi truy vấn (ElasticSearch.cs:2205, 2266)
    public string? PhoneNumber { get; set; }              // Số điện thoại (EQRController.cs:16136, ElasticSearch.cs:2267)
    public string? UserID { get; set; }                   // Mã người dùng (EQRController.cs:16137, ElasticSearch.cs:2272)
    public string? AccessToken { get; set; }              // Token truy cập (ElasticSearch.cs:2263)
    public string? NetworkID { get; set; }                // Mã mạng (ElasticSearch.cs:2261)
    public string? OrgID { get; set; }                    // Mã tổ chức (ElasticSearch.cs:2262)
    public string? WAUserCode { get; set; }               // Người dùng WA (ElasticSearch.cs:2259)
    public string? WAUserPassword { get; set; }           // Mật khẩu người dùng WA (ElasticSearch.cs:2260)
    public string? MapLongitude { get; set; }             // Kinh độ (ElasticSearch.cs:2268)
    public string? MapLatitude { get; set; }              // Vĩ độ (ElasticSearch.cs:2269)
    public string? Location { get; set; }                 // Vị trí (ElasticSearch.cs:2270)
    public string? FlagIncreaseQueryCount { get; set; }   // Cờ tăng số lần truy vấn (ElasticSearch.cs:2271)
    public int? pageIndex { get; set; }                   // Trang bắt đầu (EQRController.cs:16145)
    public int? pageSize { get; set; }                    // Kích thước trang (EQRController.cs:16146)
}

/// <summary>
/// DTO phản hồi trả về cho client — chứa Success và Data (RT_ELTS_eTEM_Search).
/// RT_ELTS_eTEM_Search dùng CHUNG với InBrandControllerPort (#405) — KHÔNG định nghĩa lại.
/// </summary>
public class EQREltsEtemSearchResp
{
    public bool Success { get; set; } = true;
    public RT_ELTS_eTEM_Search? Data { get; set; }
}

/// <summary>
/// Controller Port 1:1 cho EQRController (hạng mục #108, #174, #166).
/// </summary>
[ApiController]
[Route("api/eqr")]
public class EQRControllerPort : ControllerBase
{
    /// <summary>
    /// Port 1:1 action WA_MstSv_Mst_Network_GetMinVersion (EQRController.cs:6326-6475).
    /// Lấy phiên bản tối thiểu MinVersion của NetworkID từ bảng MstSv_Mst_Network, trả về trong Remark của c_K_DT_Sys.
    /// </summary>
    [HttpPost("wa_mstsv_mst_network_getminversion")]
    public async Task<IActionResult> WA_MstSv_Mst_Network_GetMinVersion(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EQRMstSvMstNetworkGetMinVersionReq? req,
        [FromServices] AppDbContext db)
    {
        // 1. Nhận tham số header theo nguồn EQRController.cs:6345-6353
        var headerNetworkId = Request?.Headers["networkId"].FirstOrDefault();
        var headerOrgId = Request?.Headers["orgId"].FirstOrDefault();
        var headerAppVer = Request?.Headers["AppVer"].FirstOrDefault();
        var headerAuth = Request?.Headers["Authorization"].FirstOrDefault();

        // Ưu tiên NetworkID truyền từ body hoặc header networkId (EQRController.cs:6351, 6415)
        var networkId = !string.IsNullOrEmpty(req?.NetworkID) ? req.NetworkID : headerNetworkId;

        // 2. Bỏ qua CheckAuthorize và CheckGatewayAuthentication (ghi GAP theo PORT_RULES_MiniGate.md §1 & §2.5)

        // 3. Xác định Tid: dùng Tid từ client gửi hoặc tự sinh mã timestamp hợp lệ (EQRController.cs:6360)
        var tid = !string.IsNullOrWhiteSpace(req?.Tid)
            ? req.Tid
            : DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff");

        // 4. Gọi nghiệp vụ cốt lõi theo nguồn EQR.cs:13707-13828 (EQR_MstSv_Mst_Network_GetMinVersion)
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // Truy vấn SQL nguồn EQR.cs:13758-13766:
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

            // 5. Kết quả thành công theo nguồn EQR.cs:13786 và EQRController.cs:6436-6440:
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

            return Ok(new EQRMstSvMstNetworkGetMinVersionResp
            {
                Success = true,
                Data = rtResult
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Nguồn: EQR.cs:13726, 13805 (strErrorCodeDefault = TError.ErridNTVAN.MstSv_Mst_Network_GetMinVersion)
            return PortResult.Fail("MstSv_Mst_Network_GetMinVersion", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action WA_Mst_NotifyUpdVersion_Get (EQRController.cs:17373-17515).
    /// Kiểm tra và lấy thông tin thông báo cập nhật phiên bản ứng dụng theo NetworkID và AppVersion.
    /// </summary>
    [HttpPost("wa_mst_notifyupdversion_get")]
    public async Task<IActionResult> WA_Mst_NotifyUpdVersion_Get(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EQRMstNotifyUpdVersionGetReq? req,
        [FromServices] AppDbContext db)
    {
        // 1. Nhận tham số header theo nguồn EQRController.cs:17395-17402
        var headerNetworkId = Request?.Headers["networkId"].FirstOrDefault();
        var headerOrgId = Request?.Headers["orgId"].FirstOrDefault();
        var headerAppVer = Request?.Headers["AppVer"].FirstOrDefault();
        var headerAuth = Request?.Headers["Authorization"].FirstOrDefault();

        // Ưu tiên NetworkID / AppVersion truyền từ body hoặc header (EQRController.cs:17400-17402, 17449-17451)
        var networkId = !string.IsNullOrEmpty(headerNetworkId) ? headerNetworkId : req?.NetworkID;
        var appVersion = !string.IsNullOrEmpty(headerAppVer) ? headerAppVer : req?.AppVersion;
        var orgId = !string.IsNullOrEmpty(headerOrgId) ? headerOrgId : req?.OrgID;

        // 2. Bỏ qua CheckAuthorize và CheckGatewayAuthentication (ghi GAP theo PORT_RULES_MiniGate.md §1 & §2.5)

        // 3. Xác định Tid: dùng Tid từ client gửi hoặc tự sinh mã timestamp hợp lệ (EQRController.cs:17377, 17431)
        var tid = !string.IsNullOrWhiteSpace(req?.Tid)
            ? req.Tid
            : DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff");

        // 4. Mở transaction và thực hiện nghiệp vụ theo EQR.cs:21983-22055 (EQR_Mst_NotifyUpdVersion_Get)
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
                    // (EQR.cs:22018-22025)
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

                    // Left join Mst_NotifyUpdVersion f on t.NotifyNo = f.NotifyNo (EQR.cs:22034)
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

            // 5. Kết quả thành công theo nguồn EQR.cs:21915-21921 và EQRController.cs:17470-17478
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

            return Ok(new EQRMstNotifyUpdVersionGetResp
            {
                Success = true,
                Data = rtResult
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Nguồn: EQR.cs:21971, 22067 (strErrorCodeDefault = TError.ErridNTVAN.Mst_NotifyUpdVersion_Get)
            return PortResult.Fail("Mst_NotifyUpdVersion_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action WA_ELTS_eTEM_Search (EQRController.cs:16097-16246).
    /// Kiểm tra tên miền của URI trong Mst_DomainConfig; URI không phải URL -> ERR_IU, tên miền không cấu hình -> ERR_AUS.
    /// Nhánh hợp lệ gọi dịch vụ ngoài ElasticSearch (GAP — không mô phỏng).
    /// </summary>
    [HttpPost("wa_elts_etem_search")]
    public async Task<IActionResult> WA_ELTS_eTEM_Search(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EQREltsEtemSearchReq? req,
        [FromServices] AppDbContext db)
    {
        // 1. Tid: gate gán GetNextTId() (EQRController.cs:16163) — ở đích dùng Tid client gửi, thiếu thì sinh theo mốc thời gian.
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

        // 5. Phong bì kết quả (EQRController.cs:16198: resultEntry.Data = objRT_ELTS_eTEM_Search)
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

        return Ok(new EQREltsEtemSearchResp
        {
            Success = true,
            Data = rt
        });
    }
}
