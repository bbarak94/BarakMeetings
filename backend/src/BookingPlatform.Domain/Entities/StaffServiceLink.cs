using BookingPlatform.Domain.Common;

namespace BookingPlatform.Domain.Entities;

/// <summary>
/// Links a staff member to services they can provide.
/// Allows overriding base price and duration per staff.
/// </summary>
public class StaffServiceLink : TenantEntity
{
    public Guid StaffMemberId { get; set; }
    public Guid ServiceId { get; set; }

    /// <summary>
    /// Override base price. Null = use service default.
    /// </summary>
    public decimal? PriceOverride { get; set; }

    /// <summary>
    /// Override duration in minutes. Null = use service default.
    /// </summary>
    public int? DurationOverride { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public StaffMember StaffMember { get; set; } = null!;
    public ServiceDefinition Service { get; set; } = null!;

    public decimal GetEffectivePrice() => PriceOverride ?? Service.BasePrice;
    public int GetEffectiveDuration() => DurationOverride ?? Service.BaseDurationMinutes;
}
