using BookingPlatform.Domain.Common;
using BookingPlatform.Domain.Enums;

namespace BookingPlatform.Domain.Entities;

/// <summary>
/// Represents a business/organization on the platform.
/// Not tenant-scoped itself - this IS the tenant.
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // URL-friendly unique identifier
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public string Currency { get; set; } = "USD";

    public BusinessTemplate Template { get; set; } = BusinessTemplate.Generic;
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;

    /// <summary>
    /// JSON configuration for theming, settings, etc.
    /// </summary>
    public string? ConfigJson { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<TenantUser> TenantUsers { get; set; } = new List<TenantUser>();
    public ICollection<ServiceDefinition> Services { get; set; } = new List<ServiceDefinition>();
    public ICollection<StaffMember> StaffMembers { get; set; } = new List<StaffMember>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Client> Clients { get; set; } = new List<Client>();
}
