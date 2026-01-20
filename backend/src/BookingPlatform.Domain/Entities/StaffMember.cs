using BookingPlatform.Domain.Common;

namespace BookingPlatform.Domain.Entities;

/// <summary>
/// A staff member who can provide services.
/// Linked to TenantUser for authentication.
/// </summary>
public class StaffMember : TenantEntity
{
    public Guid TenantUserId { get; set; }
    public string? Title { get; set; } // e.g., "Senior Stylist", "Yoga Instructor"
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool AcceptsBookings { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    // Navigation
    public TenantUser TenantUser { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public ICollection<StaffServiceLink> StaffServiceLinks { get; set; } = new List<StaffServiceLink>();
    public ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<StaffBreak> Breaks { get; set; } = new List<StaffBreak>();
}
