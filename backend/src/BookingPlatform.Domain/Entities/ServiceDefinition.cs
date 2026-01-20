using BookingPlatform.Domain.Common;

namespace BookingPlatform.Domain.Entities;

/// <summary>
/// Defines a service offered by a tenant.
/// Capacity > 1 indicates a group class.
/// </summary>
public class ServiceDefinition : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public int BaseDurationMinutes { get; set; }

    /// <summary>
    /// Maximum participants. 1 = private session, >1 = group class
    /// </summary>
    public int Capacity { get; set; } = 1;

    /// <summary>
    /// Buffer time between appointments in minutes
    /// </summary>
    public int BufferMinutes { get; set; } = 0;

    public string? Color { get; set; } // For calendar display
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public bool IsGroupClass => Capacity > 1;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<StaffServiceLink> StaffServiceLinks { get; set; } = new List<StaffServiceLink>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
