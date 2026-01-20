using BookingPlatform.Domain.Common;

namespace BookingPlatform.Domain.Entities;

/// <summary>
/// Wait list for fully booked services/time slots.
/// </summary>
public class WaitListEntry : TenantEntity
{
    public Guid ClientId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid? StaffMemberId { get; set; } // Optional - any staff
    public DateOnly PreferredDate { get; set; }
    public TimeOnly? PreferredStartTime { get; set; }
    public TimeOnly? PreferredEndTime { get; set; }
    public bool IsNotified { get; set; } = false;
    public DateTime? NotifiedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Client Client { get; set; } = null!;
    public ServiceDefinition Service { get; set; } = null!;
    public StaffMember? StaffMember { get; set; }
}
