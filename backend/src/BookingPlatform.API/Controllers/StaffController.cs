using BookingPlatform.Domain.Interfaces;
using BookingPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StaffController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public StaffController(ApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<StaffDto>>> GetStaff([FromQuery] Guid? serviceId = null)
    {
        var query = _context.StaffMembers
            .Include(s => s.TenantUser)
                .ThenInclude(tu => tu.User)
            .Include(s => s.StaffServiceLinks)
            .Where(s => s.IsActive && s.AcceptsBookings);

        if (serviceId.HasValue)
        {
            query = query.Where(s => s.StaffServiceLinks.Any(ssl => ssl.ServiceId == serviceId));
        }

        var staff = await query
            .OrderBy(s => s.SortOrder)
            .Select(s => new StaffDto
            {
                Id = s.Id,
                FirstName = s.TenantUser.User.FirstName,
                LastName = s.TenantUser.User.LastName,
                Title = s.Title,
                Bio = s.Bio,
                AvatarUrl = s.AvatarUrl,
                ServiceIds = s.StaffServiceLinks.Select(ssl => ssl.ServiceId).ToList()
            })
            .ToListAsync();

        return Ok(staff);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StaffDto>> GetStaffMember(Guid id)
    {
        var staff = await _context.StaffMembers
            .Include(s => s.TenantUser)
                .ThenInclude(tu => tu.User)
            .Include(s => s.StaffServiceLinks)
            .Where(s => s.Id == id)
            .Select(s => new StaffDto
            {
                Id = s.Id,
                FirstName = s.TenantUser.User.FirstName,
                LastName = s.TenantUser.User.LastName,
                Title = s.Title,
                Bio = s.Bio,
                AvatarUrl = s.AvatarUrl,
                ServiceIds = s.StaffServiceLinks.Select(ssl => ssl.ServiceId).ToList()
            })
            .FirstOrDefaultAsync();

        if (staff == null)
            return NotFound();

        return Ok(staff);
    }

    [HttpGet("{id}/working-hours")]
    [AllowAnonymous]
    public async Task<ActionResult<List<WorkingHoursDto>>> GetWorkingHours(Guid id)
    {
        var workingHours = await _context.WorkingHours
            .Where(wh => wh.StaffMemberId == id && wh.IsActive)
            .OrderBy(wh => wh.DayOfWeek)
            .Select(wh => new WorkingHoursDto
            {
                DayOfWeek = wh.DayOfWeek,
                StartTime = wh.StartTime.ToString("HH:mm"),
                EndTime = wh.EndTime.ToString("HH:mm")
            })
            .ToListAsync();

        return Ok(workingHours);
    }

    [HttpGet("{id}/availability")]
    [AllowAnonymous]
    public async Task<ActionResult<List<TimeSlotDto>>> GetAvailability(
        Guid id,
        [FromQuery] Guid serviceId,
        [FromQuery] DateTime date)
    {
        // Get staff working hours for the day
        var dayOfWeek = date.DayOfWeek;
        var workingHours = await _context.WorkingHours
            .FirstOrDefaultAsync(wh =>
                wh.StaffMemberId == id &&
                wh.DayOfWeek == dayOfWeek &&
                wh.IsActive);

        if (workingHours == null)
            return Ok(new List<TimeSlotDto>()); // Not working that day

        // Get service duration
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null)
            return BadRequest("Service not found");

        // Get existing appointments for the day
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var existingAppointments = await _context.Appointments
            .Where(a =>
                a.StaffMemberId == id &&
                a.StartTimeUtc >= startOfDay &&
                a.StartTimeUtc < endOfDay &&
                a.Status != Domain.Enums.AppointmentStatus.Cancelled)
            .OrderBy(a => a.StartTimeUtc)
            .ToListAsync();

        // Generate available slots
        var slots = new List<TimeSlotDto>();
        var slotDuration = service.BaseDurationMinutes + service.BufferMinutes;
        var currentTime = startOfDay.Add(workingHours.StartTime.ToTimeSpan());
        var endTime = startOfDay.Add(workingHours.EndTime.ToTimeSpan());

        while (currentTime.AddMinutes(service.BaseDurationMinutes) <= endTime)
        {
            var slotEnd = currentTime.AddMinutes(service.BaseDurationMinutes);

            // Check if slot conflicts with existing appointments
            var hasConflict = existingAppointments.Any(a =>
                (currentTime >= a.StartTimeUtc && currentTime < a.EndTimeUtc) ||
                (slotEnd > a.StartTimeUtc && slotEnd <= a.EndTimeUtc) ||
                (currentTime <= a.StartTimeUtc && slotEnd >= a.EndTimeUtc));

            // For group classes, check capacity
            var currentAttendees = 0;
            if (service.IsGroupClass && !hasConflict)
            {
                currentAttendees = existingAppointments
                    .Count(a => a.StartTimeUtc == currentTime && a.ServiceId == serviceId);
                hasConflict = currentAttendees >= service.Capacity;
            }

            slots.Add(new TimeSlotDto
            {
                StartTime = currentTime,
                EndTime = slotEnd,
                IsAvailable = !hasConflict && currentTime > DateTime.UtcNow,
                CurrentAttendees = service.IsGroupClass ? currentAttendees : null,
                MaxCapacity = service.IsGroupClass ? service.Capacity : null
            });

            currentTime = currentTime.AddMinutes(slotDuration);
        }

        return Ok(slots);
    }
}

public record StaffDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? Title { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public List<Guid> ServiceIds { get; init; } = new();
}

public record WorkingHoursDto
{
    public DayOfWeek DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
}

public record TimeSlotDto
{
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public bool IsAvailable { get; init; }
    public int? CurrentAttendees { get; init; }
    public int? MaxCapacity { get; init; }
}
