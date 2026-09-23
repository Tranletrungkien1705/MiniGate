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
