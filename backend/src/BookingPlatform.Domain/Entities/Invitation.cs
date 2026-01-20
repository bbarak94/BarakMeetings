using BookingPlatform.Domain.Common;
using BookingPlatform.Domain.Enums;

namespace BookingPlatform.Domain.Entities;

public class Invitation : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    public TenantRole Role { get; set; } = TenantRole.Staff;
    public bool CreateAsStaff { get; set; } = true;
    public string? StaffTitle { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
    public bool IsAccepted { get; set; } = false;
    public DateTime? AcceptedAtUtc { get; set; }

    public Guid InvitedByUserId { get; set; }
    public ApplicationUser InvitedByUser { get; set; } = null!;
}
