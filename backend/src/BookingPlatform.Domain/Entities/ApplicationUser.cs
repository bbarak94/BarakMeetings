using BookingPlatform.Domain.Common;
using BookingPlatform.Domain.Enums;

namespace BookingPlatform.Domain.Entities;

/// <summary>
/// Global user identity - not tenant-scoped.
/// One user can belong to multiple tenants via TenantUser.
/// </summary>
public class ApplicationUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }
    public SystemRole SystemRole { get; set; } = SystemRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigation
    public ICollection<TenantUser> TenantUsers { get; set; } = new List<TenantUser>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
