using System.Security.Claims;
using System.Security.Cryptography;
using BookingPlatform.Application.Services;
using BookingPlatform.Domain.Entities;
using BookingPlatform.Domain.Enums;
using BookingPlatform.Domain.Interfaces;
using BookingPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;
    private readonly IEmailService _emailService;
    private readonly ILogger<UsersController> _logger;
    private readonly IConfiguration _configuration;

    public UsersController(
        ApplicationDbContext context,
        ICurrentTenantService tenantService,
        IEmailService emailService,
        ILogger<UsersController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _tenantService = tenantService;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get all tenant users (staff members with roles)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TenantUserDto>>> GetTenantUsers()
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var users = await _context.TenantUsers
            .Include(tu => tu.User)
            .Include(tu => tu.StaffMember)
            .Where(tu => tu.TenantId == tenantId.Value)
            .OrderBy(tu => tu.Role)
            .ThenBy(tu => tu.User.FirstName)
            .Select(tu => new TenantUserDto
            {
                Id = tu.Id,
                UserId = tu.UserId,
                FirstName = tu.User.FirstName,
                LastName = tu.User.LastName,
                Email = tu.User.Email,
                Role = tu.Role,
                IsActive = tu.IsActive,
                IsStaff = tu.StaffMember != null,
                StaffMemberId = tu.StaffMember != null ? tu.StaffMember.Id : null,
                LastLoginAt = tu.User.LastLoginAtUtc
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Get pending invitations for this tenant
    /// </summary>
    [HttpGet("invitations")]
    public async Task<ActionResult<List<InvitationDto>>> GetPendingInvitations()
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var invitations = await _context.Invitations
            .Include(i => i.InvitedByUser)
            .Where(i => i.TenantId == tenantId.Value && !i.IsAccepted && i.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Select(i => new InvitationDto
            {
                Id = i.Id,
                Email = i.Email,
                Role = i.Role,
                CreateAsStaff = i.CreateAsStaff,
                StaffTitle = i.StaffTitle,
                InvitedByName = $"{i.InvitedByUser.FirstName} {i.InvitedByUser.LastName}",
                CreatedAt = i.CreatedAtUtc,
                ExpiresAt = i.ExpiresAtUtc
            })
            .ToListAsync();

        return Ok(invitations);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TenantUserDto>> GetTenantUser(Guid id)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var user = await _context.TenantUsers
            .Include(tu => tu.User)
            .Include(tu => tu.StaffMember)
            .Where(tu => tu.Id == id && tu.TenantId == tenantId.Value)
            .Select(tu => new TenantUserDto
            {
                Id = tu.Id,
                UserId = tu.UserId,
                FirstName = tu.User.FirstName,
                LastName = tu.User.LastName,
                Email = tu.User.Email,
                Role = tu.Role,
                IsActive = tu.IsActive,
                IsStaff = tu.StaffMember != null,
                StaffMemberId = tu.StaffMember != null ? tu.StaffMember.Id : null,
                LastLoginAt = tu.User.LastLoginAtUtc
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Send an invitation email to a new staff member
    /// </summary>
    [HttpPost("invite")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<InvitationDto>> InviteUser([FromBody] InviteUserRequest request)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        // Get current user info
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
            return Unauthorized();

        var currentUser = await _context.Users.FindAsync(userId);
        if (currentUser == null)
            return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null)
            return BadRequest("Tenant not found");

        // Check if user already exists in this tenant
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            var existingMembership = await _context.TenantUsers
                .FirstOrDefaultAsync(tu => tu.UserId == existingUser.Id && tu.TenantId == tenantId.Value);

            if (existingMembership != null && existingMembership.IsActive)
                return BadRequest("User is already a member of this business");
        }

        // Check for existing pending invitation
        var existingInvitation = await _context.Invitations
            .FirstOrDefaultAsync(i => i.Email == request.Email && i.TenantId == tenantId.Value && !i.IsAccepted && i.ExpiresAtUtc > DateTime.UtcNow);

        if (existingInvitation != null)
            return BadRequest("An invitation has already been sent to this email");

        // Generate secure token
        var token = GenerateSecureToken();

        // Create invitation
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Email = request.Email,
            Token = token,
            Role = request.Role,
            CreateAsStaff = request.CreateAsStaff,
            StaffTitle = request.StaffTitle,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            InvitedByUserId = userId
        };

        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync();

        // Build accept invitation URL
        var frontendUrl = _configuration["Cors:AllowedOrigins"]?.Split(',').FirstOrDefault()?.Trim() ?? "http://localhost:5173";
        var acceptUrl = $"{frontendUrl}/accept-invitation?token={token}";

        // Log invitation URL for development (always log it so it can be used when email is disabled)
        _logger.LogInformation("========================================");
        _logger.LogInformation("INVITATION LINK (for development):");
        _logger.LogInformation("{AcceptUrl}", acceptUrl);
        _logger.LogInformation("========================================");

        // Check if email is enabled
        var emailEnabled = _configuration.GetValue<bool>("Email:IsEnabled");
        var emailSent = false;
        var emailError = "";

        // Send invitation email (fire and forget style, don't block the response)
        if (emailEnabled)
        {
            try
            {
                await _emailService.SendInvitationLinkEmailAsync(new InvitationLinkEmailRequest(
                    ToEmail: request.Email,
                    TenantName: tenant.Name,
                    InvitedByName: $"{currentUser.FirstName} {currentUser.LastName}",
                    Role: GetRoleDisplayName(request.Role),
                    AcceptInvitationUrl: acceptUrl
                ));

                emailSent = true;
                _logger.LogInformation("Invitation email sent to {Email} for tenant {TenantId}", request.Email, tenantId.Value);
            }
            catch (Exception ex)
            {
                emailError = ex.Message;
                _logger.LogWarning(ex, "Failed to send invitation email to {Email}, but invitation was created. Use the link logged above.", request.Email);
            }
        }

        return CreatedAtAction(nameof(GetPendingInvitations), new InvitationDto
        {
            Id = invitation.Id,
            Email = invitation.Email,
            Role = invitation.Role,
            CreateAsStaff = invitation.CreateAsStaff,
            StaffTitle = invitation.StaffTitle,
            InvitedByName = $"{currentUser.FirstName} {currentUser.LastName}",
            CreatedAt = invitation.CreatedAtUtc,
            ExpiresAt = invitation.ExpiresAtUtc,
            // Always include link so user can share manually if email fails
            InvitationLink = acceptUrl,
            EmailSent = emailSent,
            EmailError = emailError
        });
    }

    /// <summary>
    /// Resend an invitation email
    /// </summary>
    [HttpPost("invitations/{id}/resend")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult> ResendInvitation(Guid id)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var invitation = await _context.Invitations
            .Include(i => i.InvitedByUser)
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId.Value);

        if (invitation == null)
            return NotFound();

        if (invitation.IsAccepted)
            return BadRequest("Invitation has already been accepted");

        // Regenerate token and extend expiration
        invitation.Token = GenerateSecureToken();
        invitation.ExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        // Build accept invitation URL
        var frontendUrl = _configuration["Cors:AllowedOrigins"]?.Split(',').FirstOrDefault()?.Trim() ?? "http://localhost:5173";
        var acceptUrl = $"{frontendUrl}/accept-invitation?token={invitation.Token}";

        // Send invitation email
        await _emailService.SendInvitationLinkEmailAsync(new InvitationLinkEmailRequest(
            ToEmail: invitation.Email,
            TenantName: invitation.Tenant.Name,
            InvitedByName: $"{invitation.InvitedByUser.FirstName} {invitation.InvitedByUser.LastName}",
            Role: GetRoleDisplayName(invitation.Role),
            AcceptInvitationUrl: acceptUrl
        ));

        _logger.LogInformation("Invitation resent to {Email}", invitation.Email);

        return NoContent();
    }

    /// <summary>
    /// Cancel/delete a pending invitation
    /// </summary>
    [HttpDelete("invitations/{id}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult> CancelInvitation(Guid id)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var invitation = await _context.Invitations
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId.Value);

        if (invitation == null)
            return NotFound();

        if (invitation.IsAccepted)
            return BadRequest("Cannot cancel an accepted invitation");

        _context.Invitations.Remove(invitation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invitation {Id} cancelled", id);

        return NoContent();
    }

    /// <summary>
    /// Update a user's role in the tenant
    /// </summary>
    [HttpPut("{id}/role")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult> UpdateUserRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var tenantUser = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.Id == id && tu.TenantId == tenantId.Value);

        if (tenantUser == null)
            return NotFound();

        // Prevent demoting the last owner
        if (tenantUser.Role == TenantRole.Owner && request.Role != TenantRole.Owner)
        {
            var ownerCount = await _context.TenantUsers
                .CountAsync(tu => tu.TenantId == tenantId.Value && tu.Role == TenantRole.Owner && tu.IsActive);

            if (ownerCount <= 1)
                return BadRequest("Cannot demote the last owner of the tenant");
        }

        tenantUser.Role = request.Role;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {Id} role updated to {Role}", id, request.Role);

        return NoContent();
    }

    /// <summary>
    /// Deactivate a user from the tenant
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult> DeactivateUser(Guid id)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var tenantUser = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.Id == id && tu.TenantId == tenantId.Value);

        if (tenantUser == null)
            return NotFound();

        // Prevent deactivating the last owner
        if (tenantUser.Role == TenantRole.Owner)
        {
            var ownerCount = await _context.TenantUsers
                .CountAsync(tu => tu.TenantId == tenantId.Value && tu.Role == TenantRole.Owner && tu.IsActive);

            if (ownerCount <= 1)
                return BadRequest("Cannot deactivate the last owner of the tenant");
        }

        tenantUser.IsActive = false;

        // Also deactivate staff member if exists
        var staffMember = await _context.StaffMembers
            .FirstOrDefaultAsync(s => s.TenantUserId == tenantUser.Id);
        if (staffMember != null)
        {
            staffMember.IsActive = false;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {Id} deactivated from tenant", id);

        return NoContent();
    }

    /// <summary>
    /// Reactivate a user
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<ActionResult> ActivateUser(Guid id)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var tenantUser = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.Id == id && tu.TenantId == tenantId.Value);

        if (tenantUser == null)
            return NotFound();

        tenantUser.IsActive = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
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

public record TenantUserDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; init; } = string.Empty;
    public TenantRole Role { get; init; }
    public bool IsActive { get; init; }
    public bool IsStaff { get; init; }
    public Guid? StaffMemberId { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public record InvitationDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public TenantRole Role { get; init; }
    public bool CreateAsStaff { get; init; }
    public string? StaffTitle { get; init; }
    public string InvitedByName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    /// <summary>
    /// Invitation link - always included so it can be shared manually
    /// </summary>
    public string? InvitationLink { get; init; }
    /// <summary>
    /// Whether the invitation email was sent successfully
    /// </summary>
    public bool EmailSent { get; init; }
    /// <summary>
    /// Error message if email failed to send
    /// </summary>
    public string? EmailError { get; init; }
}

public record InviteUserRequest(
    string Email,
    TenantRole Role = TenantRole.Staff,
    bool CreateAsStaff = true,
    string? StaffTitle = null
);

public record UpdateRoleRequest(TenantRole Role);
