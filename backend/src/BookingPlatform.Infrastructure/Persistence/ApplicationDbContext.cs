using BookingPlatform.Domain.Common;
using BookingPlatform.Domain.Entities;
using BookingPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingPlatform.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly ICurrentTenantService _currentTenantService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentTenantService currentTenantService) : base(options)
    {
        _currentTenantService = currentTenantService;
    }

    // Global Entities (not tenant-scoped)
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    // Tenant-scoped Entities
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ServiceDefinition> Services => Set<ServiceDefinition>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<StaffServiceLink> StaffServiceLinks => Set<StaffServiceLink>();
    public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
    public DbSet<StaffBreak> StaffBreaks => Set<StaffBreak>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<WaitListEntry> WaitListEntries => Set<WaitListEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Apply Global Query Filters for tenant isolation
        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        // Get all entity types that inherit from TenantEntity
        var tenantEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(TenantEntity).IsAssignableFrom(e.ClrType) && e.ClrType != typeof(TenantEntity));

        foreach (var entityType in tenantEntityTypes)
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var tenantIdProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(TenantEntity.TenantId));
            var currentTenantId = System.Linq.Expressions.Expression.Property(
                System.Linq.Expressions.Expression.Constant(_currentTenantService),
                nameof(ICurrentTenantService.TenantId));

            // Handle nullable Guid comparison
            var hasValue = System.Linq.Expressions.Expression.Property(currentTenantId, "HasValue");
            var value = System.Linq.Expressions.Expression.Property(currentTenantId, "Value");
            var equals = System.Linq.Expressions.Expression.Equal(tenantIdProperty, value);
            var condition = System.Linq.Expressions.Expression.OrElse(
                System.Linq.Expressions.Expression.Not(hasValue),
                equals);

            var lambda = System.Linq.Expressions.Expression.Lambda(condition, parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                    if (entry.Entity.Id == Guid.Empty)
                        entry.Entity.Id = Guid.NewGuid();
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                    break;
            }
        }

        // Auto-set TenantId for new tenant entities
        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                if (_currentTenantService.TenantId.HasValue)
                {
                    entry.Entity.TenantId = _currentTenantService.TenantId.Value;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
