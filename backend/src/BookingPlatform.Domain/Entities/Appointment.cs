using BookingPlatform.Domain.Common;
using BookingPlatform.Domain.Enums;

namespace BookingPlatform.Domain.Entities;

/// <summary>
/// The core booking entity.
/// For group classes, multiple appointments can share the same time slot.
/// </summary>
public class Appointment : TenantEntity
{
    public Guid ServiceId { get; set; }
    public Guid StaffMemberId { get; set; }
    public Guid ClientId { get; set; }

    /// <summary>
    /// For group classes, links multiple bookings together.
    /// </summary>
    public Guid? GroupSessionId { get; set; }

    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Notes visible only to staff
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Notes from the customer
    /// </summary>
    public string? CustomerNotes { get; set; }

    public string? CancellationReason { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    /// <summary>
    /// For optimistic concurrency
    /// </summary>
    public uint Version { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ServiceDefinition Service { get; set; } = null!;
    public StaffMember StaffMember { get; set; } = null!;
    public Client Client { get; set; } = null!;
}
