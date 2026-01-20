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
public class ServicesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public ServicesController(ApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ServiceDto>>> GetServices()
    {
        var services = await _context.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                DurationMinutes = s.BaseDurationMinutes,
                Price = s.BasePrice,
                Capacity = s.Capacity,
                IsGroupClass = s.IsGroupClass,
                Color = s.Color
            })
            .ToListAsync();

        return Ok(services);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceDto>> GetService(Guid id)
    {
        var service = await _context.Services
            .Where(s => s.Id == id)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                DurationMinutes = s.BaseDurationMinutes,
                Price = s.BasePrice,
                Capacity = s.Capacity,
                IsGroupClass = s.IsGroupClass,
                Color = s.Color
            })
            .FirstOrDefaultAsync();

        if (service == null)
            return NotFound();

        return Ok(service);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceDto>> CreateService([FromBody] CreateServiceRequest request)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var service = new ServiceDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name,
            Description = request.Description,
            BaseDurationMinutes = request.DurationMinutes,
            BasePrice = request.Price,
            Capacity = request.Capacity,
            BufferMinutes = request.BufferMinutes,
            Color = request.Color,
            IsActive = true,
            SortOrder = request.SortOrder
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetService), new { id = service.Id }, new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            DurationMinutes = service.BaseDurationMinutes,
            Price = service.BasePrice,
            Capacity = service.Capacity,
            IsGroupClass = service.IsGroupClass,
            Color = service.Color
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateService(Guid id, [FromBody] UpdateServiceRequest request)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
            return NotFound();

        service.Name = request.Name ?? service.Name;
        service.Description = request.Description ?? service.Description;
        service.BaseDurationMinutes = request.DurationMinutes ?? service.BaseDurationMinutes;
        service.BasePrice = request.Price ?? service.BasePrice;
        service.Capacity = request.Capacity ?? service.Capacity;
        service.BufferMinutes = request.BufferMinutes ?? service.BufferMinutes;
        service.Color = request.Color ?? service.Color;
        service.IsActive = request.IsActive ?? service.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteService(Guid id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
            return NotFound();

        // Soft delete
        service.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record ServiceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DurationMinutes { get; init; }
    public decimal Price { get; init; }
    public int Capacity { get; init; }
    public bool IsGroupClass { get; init; }
    public string? Color { get; init; }
}

public record CreateServiceRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    int Capacity = 1,
    int BufferMinutes = 0,
    string? Color = null,
    int SortOrder = 0
);

public record UpdateServiceRequest(
    string? Name = null,
    string? Description = null,
    int? DurationMinutes = null,
    decimal? Price = null,
    int? Capacity = null,
    int? BufferMinutes = null,
    string? Color = null,
    bool? IsActive = null
);
