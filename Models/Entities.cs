namespace MiniGate.Models;

public class Org
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
public interface IOrgOwned { Guid OrgId { get; set; } }

/// <summary>Tuyến định tuyến: /gw/{Prefix}/... → {UpstreamBaseUrl}/...</summary>
public class GwRoute : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = "";
    public string Prefix { get; set; } = "";            // segment đầu sau /gw (vd "pim")
    public string UpstreamBaseUrl { get; set; } = "";   // vd https://minipim.onrender.com
    public bool RequireAuth { get; set; }               // cần X-Api-Key hợp lệ
    public bool IsActive { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>Client (ứng dụng gọi qua cổng) — có API key + hạn mức.</summary>
public class ApiClient : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int RateLimitPerMin { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Nhật ký mỗi request đi qua cổng.</summary>
public class RequestLog : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int? RouteId { get; set; }
    public string RouteName { get; set; } = "";
    public string ClientName { get; set; } = "anonymous";
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string UpstreamUrl { get; set; } = "";
    public int StatusCode { get; set; }
    public long LatencyMs { get; set; }
    public DateTime At { get; set; } = DateTime.Now;
}

// ─── Port 1:1 bảng MstSv_Mst_Network của 2019.9.MobileGate ───
// Bảng "sổ mạng" của cổng: mỗi NetworkID -> URL upstream hiện tại / URL mới / phiên bản mốc.
// Nguồn cột:
//   - SQL thật: idN.MobileGate.Biz/Common.cs:225-231  (NetworkID, WSUrlAddr, WSUrlAddrNew, DefaultVersion)
//   - SQL thật: idN.MobileGate.Biz/MasterServer.cs:1391-1397 (select top 1 t.* ... where t.NetworkID = @objNetworkID)
//   - Model:    idN.MobileGate.Common/Models/MstSv_Mst_Network.cs:9-32
// LƯU Ý (GAP CỦA NGUỒN): model MstSv_Mst_Network.cs KHÔNG có WSUrlAddrNew và DefaultVersion
// dù SQL ở Common.cs:229-230 có select — nên entity dưới đây là HỢP của hai nguồn.
// KHÔNG tra được tên cột trong DB_BANG.tsv/DB_COT.tsv (từ điển đó dựng từ DbDefine.cs của
// 2023.3.DMS.Sales, không chứa MstSv_*), nên tên cột lấy TRỰC TIẾP từ SQL nguồn.
public class MstSvNetwork : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string NetworkID { get; set; } = "";
    public string? NetworkName { get; set; }
    public string? GroupNetworkID { get; set; }
    public string? CoreAddr { get; set; }
    public string? PingAddr { get; set; }
    public string? XSysAddr { get; set; }
    public string? WSUrlAddr { get; set; }          // CurrLink — Common.cs:356 arrParts[0]
    public string? WSUrlAddrNew { get; set; }       // NewLink  — Common.cs:434 arrNetWorkUrlMix[1]
    public string? DefaultVersion { get; set; }     // mốc so sánh AppVersion — Common.cs:435
    public string? DBUrlAddr { get; set; }
    public string FlagActive { get; set; } = "1";   // Const.Main.BE.cs:59 Flag.Active = "1"
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? MST { get; set; }
}
