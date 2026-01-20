using BookingPlatform.Domain.Entities;

namespace BookingPlatform.Application.Services;

public interface IJwtService
{
    string GenerateAccessToken(ApplicationUser user, Guid? tenantId, string? role);
    string GenerateRefreshToken();
    (Guid UserId, Guid? TenantId)? ValidateToken(string token);
}

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User
);

public record UserInfo(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Guid? TenantId,
    string? TenantName,
    string? Role
);
