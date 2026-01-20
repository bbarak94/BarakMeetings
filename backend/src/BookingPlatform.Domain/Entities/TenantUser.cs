using BookingPlatform.Domain.Common;
using BookingPlatform.Domain.Enums;

namespace BookingPlatform.Domain.Entities;

/// <summary>
/// Links ApplicationUser to Tenant with a specific role.
/// Enables single identity across multiple businesses.
/// </summary>
public class TenantUser : TenantEntity
{
    public Guid UserId { get; set; }
    public TenantRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public StaffMember? StaffMember { get; set; }
}
