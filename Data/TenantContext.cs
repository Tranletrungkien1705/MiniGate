namespace MiniGate.Data;

public interface ITenantContext { Guid OrgId { get; set; } }

public sealed class TenantContext : ITenantContext
{
    public static readonly Guid DefaultOrgId = new("99999999-9999-9999-9999-999999999999");
    public const string DefaultApiKey = "demo-gate";
    public const string CookieName = "org_key";
    public Guid OrgId { get; set; } = DefaultOrgId;
}
