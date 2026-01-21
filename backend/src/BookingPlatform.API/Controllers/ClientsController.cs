using BookingPlatform.Domain.Entities;
using BookingPlatform.Domain.Interfaces;
using BookingPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(
        ApplicationDbContext context,
        ICurrentTenantService tenantService,
        ILogger<ClientsController> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClientDto>>> GetClients(
        [FromQuery] string? search = null,
        [FromQuery] bool? activeOnly = true)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var query = _context.Clients
            .Include(c => c.Appointments)
            .Where(c => c.TenantId == tenantId.Value) // CRITICAL: Filter by tenant
            .AsQueryable();

        if (activeOnly == true)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(searchLower) ||
                c.LastName.ToLower().Contains(searchLower) ||
                c.Email.ToLower().Contains(searchLower) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(search)));
        }

        var clients = await query
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new ClientDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Notes = c.Notes,
                AllowMarketing = c.AllowMarketing,
                IsActive = c.IsActive,
                AppointmentCount = c.Appointments.Count,
                LastVisitDate = c.Appointments
                    .Where(a => a.Status == Domain.Enums.AppointmentStatus.Completed)
                    .OrderByDescending(a => a.StartTimeUtc)
                    .Select(a => (DateTime?)a.StartTimeUtc)
                    .FirstOrDefault(),
                TotalSpent = c.Appointments
                    .Where(a => a.Status == Domain.Enums.AppointmentStatus.Completed)
                    .Sum(a => a.Price)
            })
            .ToListAsync();

        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientDto>> GetClient(Guid id)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var client = await _context.Clients
            .Include(c => c.Appointments)
            .Where(c => c.Id == id && c.TenantId == tenantId.Value) // CRITICAL: Filter by tenant
            .Select(c => new ClientDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Notes = c.Notes,
                AllowMarketing = c.AllowMarketing,
                IsActive = c.IsActive,
                AppointmentCount = c.Appointments.Count,
                LastVisitDate = c.Appointments
                    .Where(a => a.Status == Domain.Enums.AppointmentStatus.Completed)
                    .OrderByDescending(a => a.StartTimeUtc)
                    .Select(a => (DateTime?)a.StartTimeUtc)
                    .FirstOrDefault(),
                TotalSpent = c.Appointments
                    .Where(a => a.Status == Domain.Enums.AppointmentStatus.Completed)
                    .Sum(a => a.Price)
            })
            .FirstOrDefaultAsync();

        if (client == null)
            return NotFound();

        return Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> CreateClient([FromBody] CreateClientRequest request)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        // Check if email already exists for this tenant
        var existingClient = await _context.Clients
            .FirstOrDefaultAsync(c => c.Email == request.Email && c.TenantId == tenantId.Value);

        if (existingClient != null)
            return BadRequest("A client with this email already exists");

        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Notes = request.Notes,
            AllowMarketing = request.AllowMarketing,
            IsActive = true
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client {Id} created: {Email}", client.Id, client.Email);

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, new ClientDto
        {
            Id = client.Id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            PhoneNumber = client.PhoneNumber,
            Notes = client.Notes,
            AllowMarketing = client.AllowMarketing,
            IsActive = client.IsActive,
            AppointmentCount = 0,
            TotalSpent = 0
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateClient(Guid id, [FromBody] UpdateClientRequest request)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value);
        if (client == null)
            return NotFound();

        if (request.FirstName != null) client.FirstName = request.FirstName;
        if (request.LastName != null) client.LastName = request.LastName;
        if (request.Email != null) client.Email = request.Email;
        if (request.PhoneNumber != null) client.PhoneNumber = request.PhoneNumber;
        if (request.Notes != null) client.Notes = request.Notes;
        if (request.AllowMarketing.HasValue) client.AllowMarketing = request.AllowMarketing.Value;
        if (request.IsActive.HasValue) client.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteClient(Guid id)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value);
        if (client == null)
            return NotFound();

        // Soft delete
        client.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/appointments")]
    public async Task<ActionResult<List<ClientAppointmentDto>>> GetClientAppointments(Guid id)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        // First verify client belongs to tenant
        var clientExists = await _context.Clients
            .AnyAsync(c => c.Id == id && c.TenantId == tenantId.Value);
        if (!clientExists)
            return NotFound();

        var appointments = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.StaffMember)
                .ThenInclude(s => s.TenantUser)
                    .ThenInclude(tu => tu.User)
            .Where(a => a.ClientId == id)
            .OrderByDescending(a => a.StartTimeUtc)
            .Select(a => new ClientAppointmentDto
            {
                Id = a.Id,
                ServiceName = a.Service.Name,
                StaffName = a.StaffMember.TenantUser.User.FirstName + " " + a.StaffMember.TenantUser.User.LastName,
                StartTime = a.StartTimeUtc,
                EndTime = a.EndTimeUtc,
                Price = a.Price,
                Status = a.Status
            })
            .ToListAsync();

        return Ok(appointments);
    }
}

public record ClientDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Notes { get; init; }
    public bool AllowMarketing { get; init; }
    public bool IsActive { get; init; }
    public int AppointmentCount { get; init; }
    public DateTime? LastVisitDate { get; init; }
    public decimal TotalSpent { get; init; }
}

public record CreateClientRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber = null,
    string? Notes = null,
    bool AllowMarketing = false
);

public record UpdateClientRequest(
    string? FirstName = null,
    string? LastName = null,
    string? Email = null,
    string? PhoneNumber = null,
    string? Notes = null,
    bool? AllowMarketing = null,
    bool? IsActive = null
);

public record ClientAppointmentDto
{
    public Guid Id { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string StaffName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public decimal Price { get; init; }
    public Domain.Enums.AppointmentStatus Status { get; init; }
}
