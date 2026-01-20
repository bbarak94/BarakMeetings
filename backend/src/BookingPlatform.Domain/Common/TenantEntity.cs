namespace BookingPlatform.Domain.Common;

/// <summary>
/// Base entity for all tenant-scoped data.
/// Global Query Filters in EF Core will filter by TenantId.
/// </summary>
public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
