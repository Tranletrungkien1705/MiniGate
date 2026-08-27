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

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("minigate");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();
        b.Entity<GwRoute>(e => { e.HasIndex(x => new { x.OrgId, x.Prefix }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<ApiClient>(e => { e.HasIndex(x => x.ApiKey).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<RequestLog>(e => { e.HasIndex(x => x.At); e.HasQueryFilter(x => x.OrgId == _orgId); });
    }

    public override int SaveChanges() { StampOrg(); return base.SaveChanges(); }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) { StampOrg(); return base.SaveChangesAsync(ct); }
    private void StampOrg()
    {
        foreach (var e in ChangeTracker.Entries<IOrgOwned>())
            if (e.State == EntityState.Added && e.Entity.OrgId == Guid.Empty) e.Entity.OrgId = _orgId;
    }
}
