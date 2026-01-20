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
public class AppointmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        ApplicationDbContext context,
        ICurrentTenantService tenantService,
        ILogger<AppointmentsController> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointments(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? staffId = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] AppointmentStatus? status = null)
    {
        var query = _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Client)
            .Include(a => a.StaffMember)
                .ThenInclude(s => s.TenantUser)
                    .ThenInclude(tu => tu.User)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.StartTimeUtc >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(a => a.StartTimeUtc <= endDate.Value);
        if (staffId.HasValue)
            query = query.Where(a => a.StaffMemberId == staffId.Value);
        if (clientId.HasValue)
            query = query.Where(a => a.ClientId == clientId.Value);
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var appointments = await query
            .OrderBy(a => a.StartTimeUtc)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                ServiceId = a.ServiceId,
                ServiceName = a.Service.Name,
                ServiceColor = a.Service.Color,
                StaffId = a.StaffMemberId,
                StaffName = a.StaffMember.TenantUser.User.FirstName + " " + a.StaffMember.TenantUser.User.LastName,
                ClientId = a.ClientId,
                ClientName = a.Client.FirstName + " " + a.Client.LastName,
                ClientEmail = a.Client.Email,
                StartTime = a.StartTimeUtc,
                EndTime = a.EndTimeUtc,
                DurationMinutes = a.DurationMinutes,
                Price = a.Price,
                Status = a.Status,
                CustomerNotes = a.CustomerNotes,
                InternalNotes = a.InternalNotes
            })
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(Guid id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Client)
            .Include(a => a.StaffMember)
                .ThenInclude(s => s.TenantUser)
                    .ThenInclude(tu => tu.User)
            .Where(a => a.Id == id)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                ServiceId = a.ServiceId,
                ServiceName = a.Service.Name,
                ServiceColor = a.Service.Color,
                StaffId = a.StaffMemberId,
                StaffName = a.StaffMember.TenantUser.User.FirstName + " " + a.StaffMember.TenantUser.User.LastName,
                ClientId = a.ClientId,
                ClientName = a.Client.FirstName + " " + a.Client.LastName,
                ClientEmail = a.Client.Email,
                StartTime = a.StartTimeUtc,
                EndTime = a.EndTimeUtc,
                DurationMinutes = a.DurationMinutes,
                Price = a.Price,
                Status = a.Status,
                CustomerNotes = a.CustomerNotes,
                InternalNotes = a.InternalNotes
            })
            .FirstOrDefaultAsync();

        if (appointment == null)
            return NotFound();

        return Ok(appointment);
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        // Validate service exists
        var service = await _context.Services.FindAsync(request.ServiceId);
        if (service == null)
            return BadRequest("Service not found");

        // Validate staff exists and provides this service
        var staff = await _context.StaffMembers
            .Include(s => s.StaffServiceLinks)
            .FirstOrDefaultAsync(s => s.Id == request.StaffId);
        if (staff == null)
            return BadRequest("Staff member not found");
        if (!staff.StaffServiceLinks.Any(ssl => ssl.ServiceId == request.ServiceId))
            return BadRequest("Staff member does not provide this service");

        // Get or create client
        Client? client;
        if (request.ClientId.HasValue)
        {
            client = await _context.Clients.FindAsync(request.ClientId.Value);
            if (client == null)
                return BadRequest("Client not found");
        }
        else if (request.ClientEmail != null)
        {
            client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Email == request.ClientEmail);

            if (client == null)
            {
                client = new Client
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    FirstName = request.ClientFirstName ?? "Guest",
                    LastName = request.ClientLastName ?? "",
                    Email = request.ClientEmail,
                    PhoneNumber = request.ClientPhone,
                    IsActive = true
                };
                _context.Clients.Add(client);
            }
        }
        else
        {
            return BadRequest("Client information required");
        }

        // Check for conflicts
        var endTime = request.StartTime.AddMinutes(service.BaseDurationMinutes);
        var hasConflict = await _context.Appointments
            .AnyAsync(a =>
                a.StaffMemberId == request.StaffId &&
                a.Status != AppointmentStatus.Cancelled &&
                ((request.StartTime >= a.StartTimeUtc && request.StartTime < a.EndTimeUtc) ||
                 (endTime > a.StartTimeUtc && endTime <= a.EndTimeUtc)));

        if (hasConflict && !service.IsGroupClass)
        {
            return Conflict("Time slot is not available");
        }

        // For group classes, check capacity
        if (service.IsGroupClass)
        {
            var currentAttendees = await _context.Appointments
                .CountAsync(a =>
                    a.ServiceId == service.Id &&
                    a.StaffMemberId == request.StaffId &&
                    a.StartTimeUtc == request.StartTime &&
                    a.Status != AppointmentStatus.Cancelled);

            if (currentAttendees >= service.Capacity)
            {
                return Conflict("Class is at full capacity");
            }
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            ServiceId = request.ServiceId,
            StaffMemberId = request.StaffId,
            ClientId = client.Id,
            StartTimeUtc = request.StartTime,
            EndTimeUtc = endTime,
            DurationMinutes = service.BaseDurationMinutes,
            Price = service.BasePrice,
            Status = AppointmentStatus.Pending,
            CustomerNotes = request.Notes
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment {Id} created for {Email}", appointment.Id, client.Email);

        return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, new AppointmentDto
        {
            Id = appointment.Id,
            ServiceId = service.Id,
            ServiceName = service.Name,
            ServiceColor = service.Color,
            StaffId = staff.Id,
            ClientId = client.Id,
            ClientName = client.FullName,
            ClientEmail = client.Email,
            StartTime = appointment.StartTimeUtc,
            EndTime = appointment.EndTimeUtc,
            DurationMinutes = appointment.DurationMinutes,
            Price = appointment.Price,
            Status = appointment.Status,
            CustomerNotes = appointment.CustomerNotes
        });
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            return NotFound();

        appointment.Status = request.Status;

        if (request.Status == AppointmentStatus.Cancelled)
        {
            appointment.CancellationReason = request.Reason;
            appointment.CancelledAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Appointment {Id} status updated to {Status}", id, request.Status);

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAppointment(Guid id, [FromBody] UpdateAppointmentRequest request)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            return NotFound();

        if (request.StartTime.HasValue)
        {
            var service = await _context.Services.FindAsync(appointment.ServiceId);
            appointment.StartTimeUtc = request.StartTime.Value;
            appointment.EndTimeUtc = request.StartTime.Value.AddMinutes(service?.BaseDurationMinutes ?? appointment.DurationMinutes);
        }

        if (request.StaffId.HasValue)
            appointment.StaffMemberId = request.StaffId.Value;

        if (request.InternalNotes != null)
            appointment.InternalNotes = request.InternalNotes;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAppointment(Guid id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            return NotFound();

        // Soft delete via cancellation
        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAtUtc = DateTime.UtcNow;
        appointment.CancellationReason = "Deleted by staff";

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<List<AppointmentDto>>> GetUpcoming([FromQuery] int limit = 10)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Client)
            .Include(a => a.StaffMember)
                .ThenInclude(s => s.TenantUser)
                    .ThenInclude(tu => tu.User)
            .Where(a => a.StartTimeUtc >= DateTime.UtcNow &&
                   a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.StartTimeUtc)
            .Take(limit)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                ServiceId = a.ServiceId,
                ServiceName = a.Service.Name,
                ServiceColor = a.Service.Color,
                StaffId = a.StaffMemberId,
                StaffName = a.StaffMember.TenantUser.User.FirstName + " " + a.StaffMember.TenantUser.User.LastName,
                ClientId = a.ClientId,
                ClientName = a.Client.FirstName + " " + a.Client.LastName,
                ClientEmail = a.Client.Email,
                StartTime = a.StartTimeUtc,
                EndTime = a.EndTimeUtc,
                DurationMinutes = a.DurationMinutes,
                Price = a.Price,
                Status = a.Status,
                CustomerNotes = a.CustomerNotes
            })
            .ToListAsync();

        return Ok(appointments);
    }
}

public record AppointmentDto
{
    public Guid Id { get; init; }
    public Guid ServiceId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string? ServiceColor { get; init; }
    public Guid StaffId { get; init; }
    public string StaffName { get; init; } = string.Empty;
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string ClientEmail { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public int DurationMinutes { get; init; }
    public decimal Price { get; init; }
    public AppointmentStatus Status { get; init; }
    public string? CustomerNotes { get; init; }
    public string? InternalNotes { get; init; }
}

public record CreateAppointmentRequest(
    Guid ServiceId,
    Guid StaffId,
    DateTime StartTime,
    Guid? ClientId = null,
    string? ClientEmail = null,
    string? ClientFirstName = null,
    string? ClientLastName = null,
    string? ClientPhone = null,
    string? Notes = null
);

public record UpdateStatusRequest(
    AppointmentStatus Status,
    string? Reason = null
);

public record UpdateAppointmentRequest(
    DateTime? StartTime = null,
    Guid? StaffId = null,
    string? InternalNotes = null
);
