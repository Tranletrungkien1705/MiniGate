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
/// DTO yêu cầu cho action WA_Mst_NotifyUpdVersion_Get — port 1:1 từ RQ_Mst_NotifyUpdVersion (InBrandController.cs:16903, RQ_Mst_NotifyUpdVersion.cs:9-19).
/// </summary>
public class InBrandMstNotifyUpdVersionGetReq
{
    public string? Tid { get; set; }                      // Transaction ID (InBrandController.cs:16907, 16961)
    public string? OrgID { get; set; }                    // Mã tổ chức (InBrandController.cs:16931, 16980)
    public string? NetworkID { get; set; }                // Mã mạng (InBrandController.cs:16930, 16979)
    public string? AppVersion { get; set; }               // Phiên bản ứng dụng (InBrandController.cs:16932, 16981)
    public string? AccessToken { get; set; }              // Token truy cập OAuth2 (InBrandController.cs:16924-16929, 16940)
    public string? GwUserCode { get; set; }               // Người dùng Gateway (InBrandController.cs:16937)
    public string? GwPassword { get; set; }               // Mật khẩu Gateway (InBrandController.cs:16938)
    public string? WAUserCode { get; set; }               // Người dùng WA (InBrand.cs:24273)
    public string? WAUserPassword { get; set; }           // Mật khẩu người dùng WA (InBrand.cs:24274)
    public string? Rt_Cols_Mst_NotifyUpdVersion { get; set; } // Danh sách cột trả về (RQ_Mst_NotifyUpdVersion.cs:11, InBrand.cs:24265)
}

/// <summary>
/// DTO phản hồi trả về cho client cho WA_Mst_NotifyUpdVersion_Get — chứa Success và Data (RT_Mst_NotifyUpdVersion).
/// </summary>
public class InBrandMstNotifyUpdVersionGetResp
{
    public bool Success { get; set; } = true;
    public RT_Mst_NotifyUpdVersion? Data { get; set; }
}

// ============================================================================================
// PORT 1:1 — InBrandController/WA_ELTS_eTEM_Search (hạng mục #405) của dự án nguồn 2019.9.MobileGate.
//
// Nguồn controller (tầng gate):
//   InBrandController.cs:15653-15776  WA_ELTS_eTEM_Search -> gọi _biz.WAS_ELTS_eTEM_Search_New20210813
// Nguồn biz:
//   ElasticSearch.cs:2168-2306  WAS_ELTS_eTEM_Search_New20210813 (mã lỗi mặc định TError.ErridNTVAN.WAS_ELTS_eTEM_Search)
//   ElasticSearch.cs:1436-1723  ELTS_eTEM_Search (mã lỗi mặc định TError.ErridNTVAN.ELTS_eTEM_Search)
// Nguồn model & hằng số:
//   RQ_ELTS_eTEM_Search / RT_ELTS_eTEM_Search: KHÔNG có định nghĩa trong source_excerpt.txt
//   TError.ErridNTVAN.CmSys_InvalidOutSite (ElasticSearch.cs:1666)
//
// GIỮ ĐÚNG NGUỒN & QUY ƯỚC:
//   * Route: POST /api/inbrand/wa_elts_etem_search (nhận [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]).
//   * Bước 1 (ElasticSearch.cs:2203-2250): URI chứa http:// hoặc https:// -> tra Mst_DomainConfig
//     (FlagActive = '1' and DomainValues = host); KHÔNG thấy -> ELTSBizCodeReturn = "ERR_AUS".
//     URI không phải URL -> ELTSBizCodeReturn = "ERR_IU". Cả hai nhánh đặt TrackingCode = RefInfo = URI.
//   * Bước 2 (ElasticSearch.cs:2252-2276): chỉ khi bước 1 KHÔNG lỗi mới gọi dịch vụ ngoài
//     OS_MstSv_ElasticSearchServices.Instance.WA_ELTS_eTEM_Search -> GAP, KHÔNG mô phỏng.
//   * Bỏ qua CheckAuthorize / CheckGatewayAuthentication / ProcessBizReq_OutSide theo quy ước chung của môi trường port.
// ============================================================================================

/// <summary>
/// DTO yêu cầu cho action WA_ELTS_eTEM_Search — port 1:1 từ RQ_ELTS_eTEM_Search
/// (InBrandController.cs:15653, ElasticSearch.cs:2255-2275). Định nghĩa model nguồn không có trong excerpt nên
/// chỉ khai các trường mà gate/biz THỰC SỰ đọc.
/// </summary>
public class InBrandEltsEtemSearchReq
{
    public string? Tid { get; set; }                      // Transaction ID (InBrandController.cs:15656, 15713)
    public string? URI { get; set; }                      // Chuỗi truy vấn (ElasticSearch.cs:2205, 2266)
    public string? PhoneNumber { get; set; }              // Số điện thoại (InBrandController.cs:15692, ElasticSearch.cs:2267)
    public string? UserID { get; set; }                   // Mã người dùng (InBrandController.cs:15693, ElasticSearch.cs:2272)
    public string? AccessToken { get; set; }              // Token truy cập (ElasticSearch.cs:2263)
    public string? NetworkID { get; set; }                // Mã mạng (ElasticSearch.cs:2261)
    public string? OrgID { get; set; }                    // Mã tổ chức (ElasticSearch.cs:2262)
    public string? WAUserCode { get; set; }               // Người dùng WA (ElasticSearch.cs:2259)
    public string? WAUserPassword { get; set; }           // Mật khẩu người dùng WA (ElasticSearch.cs:2260)
    public string? MapLongitude { get; set; }             // Kinh độ (ElasticSearch.cs:2268)
    public string? MapLatitude { get; set; }              // Vĩ độ (ElasticSearch.cs:2269)
    public string? Location { get; set; }                 // Vị trí (ElasticSearch.cs:2270)
    public string? FlagIncreaseQueryCount { get; set; }   // Cờ tăng số lần truy vấn (ElasticSearch.cs:2271)
    public int? pageIndex { get; set; }                   // Trang bắt đầu (InBrandController.cs:15701)
    public int? pageSize { get; set; }                    // Kích thước trang (InBrandController.cs:15702)
}

/// <summary>
/// Kết quả nghiệp vụ — port 1:1 từ RT_ELTS_eTEM_Search (định nghĩa nguồn không có trong excerpt).
/// Chỉ khai các trường mà biz nguồn THỰC SỰ gán (ElasticSearch.cs:2237-2247).
/// </summary>
public class RT_ELTS_eTEM_Search
{
    public string? ELTSBizCodeReturn { get; set; }        // Mã kết quả nghiệp vụ (ElasticSearch.cs:2237, 2245)
    public string? TrackingCode { get; set; }             // Mã tra cứu (ElasticSearch.cs:2238, 2246)
    public string? RefInfo { get; set; }                  // Thông tin tham chiếu (ElasticSearch.cs:2239, 2247)
    public c_K_DT_Sys c_K_DT_Sys { get; set; } = new();   // Phong bì hệ thống (WARTBase)
}

/// <summary>
/// DTO phản hồi trả về cho client — chứa Success và Data (RT_ELTS_eTEM_Search).
/// </summary>
public class InBrandEltsEtemSearchResp
{
    public bool Success { get; set; } = true;
    public RT_ELTS_eTEM_Search? Data { get; set; }
}

/// <summary>
/// Controller Port 1:1 cho InBrandController (hạng mục #347, #405).
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

    /// <summary>
    /// Port 1:1 action WA_Mst_NotifyUpdVersion_Get (InBrandController.cs:16903-17045).
    /// Kiểm tra và lấy thông tin thông báo cập nhật phiên bản ứng dụng theo NetworkID và AppVersion.
    /// </summary>
    [HttpPost("wa_mst_notifyupdversion_get")]
    public async Task<IActionResult> WA_Mst_NotifyUpdVersion_Get(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InBrandMstNotifyUpdVersionGetReq? req,
        [FromServices] AppDbContext db)
    {
        // 1. Nhận tham số header theo nguồn InBrandController.cs:16924-16932
        var headerNetworkId = Request?.Headers["networkId"].FirstOrDefault();
        var headerOrgId = Request?.Headers["orgId"].FirstOrDefault();
        var headerAppVer = Request?.Headers["AppVer"].FirstOrDefault();
        var headerAuth = Request?.Headers["Authorization"].FirstOrDefault();

        // Ưu tiên NetworkID / AppVersion truyền từ body hoặc header (InBrandController.cs:16930-16932, 16979-16981)
        var networkId = !string.IsNullOrEmpty(headerNetworkId) ? headerNetworkId : req?.NetworkID;
        var appVersion = !string.IsNullOrEmpty(headerAppVer) ? headerAppVer : req?.AppVersion;
        var orgId = !string.IsNullOrEmpty(headerOrgId) ? headerOrgId : req?.OrgID;

        // 2. Bỏ qua CheckAuthorize và CheckGatewayAuthentication (ghi GAP theo PORT_RULES_MiniGate.md §1 & §2.5)

        // 3. Xác định Tid: dùng Tid từ client gửi hoặc tự sinh mã timestamp hợp lệ (InBrandController.cs:16907, 16961)
        var tid = !string.IsNullOrWhiteSpace(req?.Tid)
            ? req.Tid
            : DateTime.Now.ToString("yyyyMMdd.HHmmss.ffffff");

        // 4. Mở transaction và thực hiện nghiệp vụ theo InBrand.cs:24426-24533 (Mst_NotifyUpdVersion_Get)
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
                    // (InBrand.cs:24467-24490)
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

                    // Left join Mst_NotifyUpdVersion f on t.NotifyNo = f.NotifyNo (InBrand.cs:24505-24508)
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

            // 5. Kết quả thành công theo nguồn InBrand.cs:24286-24292 và InBrandController.cs:17001-17008
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

            return Ok(new InBrandMstNotifyUpdVersionGetResp
            {
                Success = true,
                Data = rtResult
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Nguồn: InBrand.cs:24248, 24414 (strErrorCodeDefault = TError.ErridNTVAN.Mst_NotifyUpdVersion_Get)
            return PortResult.Fail("Mst_NotifyUpdVersion_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action WA_ELTS_eTEM_Search (InBrandController.cs:15653-15776).
    /// Kiểm tra tên miền của URI trong Mst_DomainConfig; URI không phải URL -> ERR_IU, tên miền không cấu hình -> ERR_AUS.
    /// Nhánh hợp lệ gọi dịch vụ ngoài ElasticSearch (GAP — không mô phỏng).
    /// </summary>
    [HttpPost("wa_elts_etem_search")]
    public async Task<IActionResult> WA_ELTS_eTEM_Search(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InBrandEltsEtemSearchReq? req,
        [FromServices] AppDbContext db)
    {
        // 1. Tid: gate gán GetNextTId() (InBrandController.cs:15713) — ở đích dùng Tid client gửi, thiếu thì sinh theo mốc thời gian.
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

        // 5. Phong bì kết quả (InBrandController.cs:15742-15743: resultEntry.Data = objRT_ELTS_eTEM_Search)
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

        return Ok(new InBrandEltsEtemSearchResp
        {
            Success = true,
            Data = rt
        });
    }
}
