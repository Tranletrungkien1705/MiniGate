using Microsoft.EntityFrameworkCore;
using MiniGate.Models;

namespace MiniGate.Data;

public class AppDbContext : DbContext
{
    private readonly Guid _orgId;
    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : base(options) => _orgId = tenant.OrgId;

    public DbSet<Org> Orgs => Set<Org>();
    public DbSet<GwRoute> Routes => Set<GwRoute>();
    public DbSet<ApiClient> Clients => Set<ApiClient>();
    public DbSet<RequestLog> Logs => Set<RequestLog>();
    public DbSet<InvoiceTemplate> InvoiceTemplates => Set<InvoiceTemplate>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLicense> InvoiceLicenses => Set<InvoiceLicense>();
    public DbSet<MstNnt> MstNnts => Set<MstNnt>();
    public DbSet<MstGovTaxId> MstGovTaxIds => Set<MstGovTaxId>();
    public DbSet<MstGovIdType> MstGovIdTypes => Set<MstGovIdType>();
    public DbSet<MstProvince> MstProvinces => Set<MstProvince>();
    public DbSet<MstDistrict> MstDistricts => Set<MstDistrict>();
    public DbSet<SysUser> SysUsers => Set<SysUser>();
    public DbSet<SysGroup> SysGroups => Set<SysGroup>();
    public DbSet<SysUserInGroup> SysUserInGroups => Set<SysUserInGroup>();
    public DbSet<InvoiceTempGroup> InvoiceTempGroups => Set<InvoiceTempGroup>();
    public DbSet<InvoiceTempGroupField> InvoiceTempGroupFields => Set<InvoiceTempGroupField>();
    public DbSet<LicOrder> LicOrders => Set<LicOrder>();
    public DbSet<LicOrderCommission> LicOrderCommissions => Set<LicOrderCommission>();
    public DbSet<MapDealerDiscount> MapDealerDiscounts => Set<MapDealerDiscount>();
    public DbSet<SysObject> SysObjects => Set<SysObject>();
    public DbSet<SysAccess> SysAccesses => Set<SysAccess>();
    public DbSet<SysObjectInModule> SysObjectInModules => Set<SysObjectInModule>();
    public DbSet<MstVatRate> MstVatRates => Set<MstVatRate>();
    public DbSet<MstDealer> MstDealers => Set<MstDealer>();
    public DbSet<MstPaymentMethod> MstPaymentMethods => Set<MstPaymentMethod>();
    public DbSet<MstInvoiceType> MstInvoiceTypes => Set<MstInvoiceType>();
    public DbSet<MstNntType> MstNntTypes => Set<MstNntType>();
    public DbSet<SysSolution> SysSolutions => Set<SysSolution>();
    public DbSet<SysModule> SysModules => Set<SysModule>();
    public DbSet<InosMstBizType> InosMstBizTypes => Set<InosMstBizType>();
    public DbSet<InosMstBizField> InosMstBizFields => Set<InosMstBizField>();
    public DbSet<InosMstBizSize> InosMstBizSizes => Set<InosMstBizSize>();
    public DbSet<MstSvInosOrg> MstSvInosOrgs => Set<MstSvInosOrg>();
    public DbSet<MstSvMstNetwork> MstSvMstNetworks => Set<MstSvMstNetwork>();
    public DbSet<MstSvInosUser> MstSvInosUsers => Set<MstSvInosUser>();
    public DbSet<NotifyNotify> NotifyNotifies => Set<NotifyNotify>();
    public DbSet<NotifyNotifyDtl> NotifyNotifyDtls => Set<NotifyNotifyDtl>();
    public DbSet<MstCountry> MstCountries => Set<MstCountry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("minigate");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();
        b.Entity<GwRoute>(e => { e.HasIndex(x => new { x.OrgId, x.Prefix }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<ApiClient>(e => { e.HasIndex(x => x.ApiKey).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<RequestLog>(e => { e.HasIndex(x => x.At); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<InvoiceTemplate>(e => { e.HasIndex(x => new { x.OrgId, x.TInvoiceCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<Invoice>(e => { e.HasIndex(x => new { x.OrgId, x.TInvoiceCode, x.InvoiceNo }); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<InvoiceLicense>(e => { e.HasIndex(x => new { x.OrgId, x.MST }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstNnt>(e => { e.HasIndex(x => new { x.OrgId, x.MST }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstGovTaxId>(e => { e.HasIndex(x => new { x.OrgId, x.GovTaxID }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstGovIdType>(e => { e.HasIndex(x => new { x.OrgId, x.GovIDType }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstProvince>(e => { e.HasIndex(x => new { x.OrgId, x.ProvinceCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstDistrict>(e => { e.HasIndex(x => new { x.OrgId, x.ProvinceCode, x.DistrictCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<SysUser>(e => { e.HasIndex(x => new { x.OrgId, x.UserCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<SysGroup>(e => { e.HasIndex(x => new { x.OrgId, x.GroupCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<SysUserInGroup>(e => { e.HasIndex(x => new { x.OrgId, x.UserCode, x.GroupCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<InvoiceTempGroup>(e => { e.HasIndex(x => new { x.OrgId, x.InvoiceTGroupCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<InvoiceTempGroupField>(e => { e.HasIndex(x => new { x.OrgId, x.InvoiceTGroupCode, x.DBFieldName }); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<LicOrder>(e => { e.HasIndex(x => new { x.OrgId, x.OrderId }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<LicOrderCommission>(e => { e.HasIndex(x => new { x.OrgId, x.OrderId }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MapDealerDiscount>(e => { e.HasIndex(x => new { x.OrgId, x.DLCode, x.DiscountCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<SysObject>(e => { e.HasIndex(x => new { x.OrgId, x.ObjectCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<SysAccess>(e => { e.HasIndex(x => new { x.OrgId, x.GroupCode, x.ObjectCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<SysObjectInModule>(e => { e.HasIndex(x => new { x.OrgId, x.ObjectCode, x.ModuleCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstVatRate>(e => { e.HasIndex(x => new { x.OrgId, x.VATRateCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstDealer>(e => { e.HasIndex(x => new { x.OrgId, x.DLCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstPaymentMethod>(e => { e.HasIndex(x => new { x.OrgId, x.PaymentMethodCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstInvoiceType>(e => { e.HasIndex(x => new { x.OrgId, x.InvoiceType }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstNntType>(e => { e.HasIndex(x => new { x.OrgId, x.NNTType }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<SysSolution>(e => { e.HasIndex(x => new { x.OrgId, x.SolutionCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<SysModule>(e => { e.HasIndex(x => new { x.OrgId, x.ModuleCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<InosMstBizType>(e => { e.HasIndex(x => new { x.OrgId, x.BizType }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<InosMstBizField>(e => { e.HasIndex(x => new { x.OrgId, x.BizFieldCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<InosMstBizSize>(e => { e.HasIndex(x => new { x.OrgId, x.BizSizeCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstSvInosOrg>(e => { e.HasIndex(x => new { x.OrgId, x.MST, x.InosId }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstSvMstNetwork>(e => { e.HasIndex(x => new { x.OrgId, x.NetworkID }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstSvInosUser>(e => { e.HasIndex(x => new { x.OrgId, x.MST, x.Email }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<NotifyNotify>(e => { e.HasIndex(x => new { x.OrgId, x.NotifyNo }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<NotifyNotifyDtl>(e => { e.HasIndex(x => new { x.OrgId, x.NotifyNo, x.UserCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstCountry>(e => { e.HasIndex(x => new { x.OrgId, x.CountryCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
    }

    public override int SaveChanges() { StampOrg(); return base.SaveChanges(); }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) { StampOrg(); return base.SaveChangesAsync(ct); }
    private void StampOrg()
    {
        foreach (var e in ChangeTracker.Entries<IOrgOwned>())
            if (e.State == EntityState.Added && e.Entity.OrgId == Guid.Empty) e.Entity.OrgId = _orgId;
    }
}
