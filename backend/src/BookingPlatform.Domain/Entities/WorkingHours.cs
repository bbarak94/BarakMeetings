using BookingPlatform.Domain.Common;

namespace BookingPlatform.Domain.Entities;

/// <summary>
/// Defines working hours for a staff member on a specific day.
/// </summary>
public class WorkingHours : TenantEntity
{
    public Guid StaffMemberId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public StaffMember StaffMember { get; set; } = null!;
}
