using BookingPlatform.Domain.Common;

namespace BookingPlatform.Domain.Entities;

/// <summary>
/// A client/customer of a tenant. Can optionally be linked to an ApplicationUser.
/// </summary>
public class Client : TenantEntity
{
    public Guid? UserId { get; set; } // Optional link to registered user
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Notes { get; set; }
    public bool AllowMarketing { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigation
    public ApplicationUser? User { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
