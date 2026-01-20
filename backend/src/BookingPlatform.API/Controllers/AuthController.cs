using BookingPlatform.Application.Services;
using BookingPlatform.Domain.Entities;
using BookingPlatform.Domain.Enums;
using BookingPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext context,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Get user's tenant membership
        var tenantUser = await _context.TenantUsers
            .Include(tu => tu.Tenant)
            .FirstOrDefaultAsync(tu => tu.UserId == user.Id && tu.IsActive);

        var tenantId = tenantUser?.TenantId;
        var role = tenantUser?.Role.ToString();
        var tenantName = tenantUser?.Tenant?.Name;

        var accessToken = _jwtService.GenerateAccessToken(user, tenantId, role);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        return Ok(new TokenResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60),
            new UserInfo(user.Id, user.Email, user.FirstName, user.LastName, tenantId, tenantName, role)
        ));
    }

    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> Register([FromBody] RegisterRequest request)
    {
        // Check if email exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { message = "Email already registered" });
        }

        // Create user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            EmailConfirmed = false
        };
        _context.Users.Add(user);

        // If tenant slug provided, create tenant and make user owner
        Guid? tenantId = null;
        string? tenantName = null;
        string? role = null;

        if (!string.IsNullOrEmpty(request.BusinessName))
        {
            var slug = GenerateSlug(request.BusinessName);

            // Check slug uniqueness
            if (await _context.Tenants.AnyAsync(t => t.Slug == slug))
            {
                slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";
            }

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.BusinessName,
                Slug = slug,
                Template = request.BusinessType ?? BusinessTemplate.Generic,
                Plan = SubscriptionPlan.Free,
                IsActive = true
            };
            _context.Tenants.Add(tenant);

            var tenantUser = new TenantUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = user.Id,
                Role = TenantRole.Owner,
                IsActive = true
            };
            _context.TenantUsers.Add(tenantUser);

            tenantId = tenant.Id;
            tenantName = tenant.Name;
            role = TenantRole.Owner.ToString();
        }

        await _context.SaveChangesAsync();

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user, tenantId, role);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New user {Email} registered", user.Email);

        return Ok(new TokenResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60),
            new UserInfo(user.Id, user.Email, user.FirstName, user.LastName, tenantId, tenantName, role)
        ));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked);

        if (storedToken == null || storedToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        // Revoke old token
        storedToken.IsRevoked = true;

        var user = storedToken.User;

        // Get tenant info
        var tenantUser = await _context.TenantUsers
            .Include(tu => tu.Tenant)
            .FirstOrDefaultAsync(tu => tu.UserId == user.Id && tu.IsActive);

        var tenantId = tenantUser?.TenantId;
        var role = tenantUser?.Role.ToString();
        var tenantName = tenantUser?.Tenant?.Name;

        // Generate new tokens
        var accessToken = _jwtService.GenerateAccessToken(user, tenantId, role);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return Ok(new TokenResponse(
            accessToken,
            newRefreshToken,
            DateTime.UtcNow.AddMinutes(60),
            new UserInfo(user.Id, user.Email, user.FirstName, user.LastName, tenantId, tenantName, role)
        ));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult> Logout([FromBody] RefreshRequest request)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (token != null)
        {
            token.IsRevoked = true;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var tenantUser = await _context.TenantUsers
            .Include(tu => tu.Tenant)
            .FirstOrDefaultAsync(tu => tu.UserId == userId && tu.IsActive);

        return Ok(new UserInfo(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            tenantUser?.TenantId,
            tenantUser?.Tenant?.Name,
            tenantUser?.Role.ToString()
        ));
    }

    /// <summary>
    /// Validate an invitation token and return invitation details
    /// </summary>
    [HttpGet("invitation/{token}")]
    public async Task<ActionResult<InvitationInfoResponse>> GetInvitationInfo(string token)
    {
        var invitation = await _context.Invitations
            .Include(i => i.Tenant)
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsAccepted);

        if (invitation == null)
            return NotFound(new { message = "Invitation not found or already used" });

        if (invitation.ExpiresAtUtc < DateTime.UtcNow)
            return BadRequest(new { message = "Invitation has expired" });

        // Check if user already exists with this email
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == invitation.Email);

        return Ok(new InvitationInfoResponse(
            invitation.Email,
            invitation.Tenant.Name,
            $"{invitation.InvitedByUser.FirstName} {invitation.InvitedByUser.LastName}",
            GetRoleDisplayName(invitation.Role),
            invitation.ExpiresAtUtc,
            existingUser != null
        ));
    }

    /// <summary>
    /// Accept an invitation and create/link user account
    /// </summary>
    [HttpPost("invitation/{token}/accept")]
    public async Task<ActionResult<TokenResponse>> AcceptInvitation(string token, [FromBody] AcceptInvitationRequest request)
    {
        var invitation = await _context.Invitations
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsAccepted);

        if (invitation == null)
            return NotFound(new { message = "Invitation not found or already used" });

        if (invitation.ExpiresAtUtc < DateTime.UtcNow)
            return BadRequest(new { message = "Invitation has expired" });

        ApplicationUser user;
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == invitation.Email);

        if (existingUser != null)
        {
            // Existing user - verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, existingUser.PasswordHash))
                return Unauthorized(new { message = "Invalid password" });

            user = existingUser;
        }
        else
        {
            // New user - create account
            if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName))
                return BadRequest(new { message = "First name and last name are required for new users" });

            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = invitation.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true,
                EmailConfirmed = true // They clicked the email link, so email is confirmed
            };
            _context.Users.Add(user);
        }

        // Check if already a member of this tenant (e.g., deactivated)
        var existingMembership = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.UserId == user.Id && tu.TenantId == invitation.TenantId);

        TenantUser tenantUser;
        if (existingMembership != null)
        {
            existingMembership.IsActive = true;
            existingMembership.Role = invitation.Role;
            tenantUser = existingMembership;
        }
        else
        {
            tenantUser = new TenantUser
            {
                Id = Guid.NewGuid(),
                TenantId = invitation.TenantId,
                UserId = user.Id,
                Role = invitation.Role,
                IsActive = true
            };
            _context.TenantUsers.Add(tenantUser);
        }

        // Create staff member if needed
        if (invitation.CreateAsStaff && invitation.Role == TenantRole.Staff)
        {
            var existingStaff = await _context.StaffMembers
                .FirstOrDefaultAsync(s => s.TenantUserId == tenantUser.Id);

            if (existingStaff == null)
            {
                var staffMember = new StaffMember
                {
                    Id = Guid.NewGuid(),
                    TenantId = invitation.TenantId,
                    TenantUserId = tenantUser.Id,
                    Title = invitation.StaffTitle,
                    AcceptsBookings = true,
                    IsActive = true,
                    SortOrder = 0
                };
                _context.StaffMembers.Add(staffMember);
            }
            else
            {
                existingStaff.IsActive = true;
            }
        }

        // Mark invitation as accepted
        invitation.IsAccepted = true;
        invitation.AcceptedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user, invitation.TenantId, invitation.Role.ToString());
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {Email} accepted invitation to tenant {TenantId}", user.Email, invitation.TenantId);

        return Ok(new TokenResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60),
            new UserInfo(user.Id, user.Email, user.FirstName, user.LastName, invitation.TenantId, invitation.Tenant.Name, invitation.Role.ToString())
        ));
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "");
    }

    private static string GetRoleDisplayName(TenantRole role) => role switch
    {
        TenantRole.Owner => "Owner",
        TenantRole.Admin => "Admin",
        TenantRole.Staff => "Staff Member",
        TenantRole.Receptionist => "Receptionist",
        _ => "Team Member"
    };
}

public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? BusinessName = null,
    BusinessTemplate? BusinessType = null
);

public record RefreshRequest(string RefreshToken);

public record InvitationInfoResponse(
    string Email,
    string TenantName,
    string InvitedByName,
    string Role,
    DateTime ExpiresAt,
    bool UserExists
);

public record AcceptInvitationRequest(
    string Password,
    string? FirstName = null,
    string? LastName = null
);
