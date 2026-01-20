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
public class AnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        ApplicationDbContext context,
        ICurrentTenantService tenantService,
        ILogger<AnalyticsController> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard overview with key metrics
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardMetrics>> GetDashboard([FromQuery] int days = 30)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var startDate = DateTime.UtcNow.AddDays(-days);
        var previousStartDate = startDate.AddDays(-days);

        // Current period appointments
        var currentAppointments = await _context.Appointments
            .Where(a => a.TenantId == tenantId && a.CreatedAtUtc >= startDate)
            .ToListAsync();

        // Previous period for comparison
        var previousAppointments = await _context.Appointments
            .Where(a => a.TenantId == tenantId &&
                   a.CreatedAtUtc >= previousStartDate &&
                   a.CreatedAtUtc < startDate)
            .ToListAsync();

        // Calculate metrics
        var totalBookings = currentAppointments.Count;
        var previousBookings = previousAppointments.Count;
        var bookingsGrowth = previousBookings > 0
            ? ((totalBookings - previousBookings) / (decimal)previousBookings) * 100
            : 0;

        var totalRevenue = currentAppointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .Sum(a => a.Price);
        var previousRevenue = previousAppointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .Sum(a => a.Price);
        var revenueGrowth = previousRevenue > 0
            ? ((totalRevenue - previousRevenue) / previousRevenue) * 100
            : 0;

        var completedCount = currentAppointments.Count(a => a.Status == AppointmentStatus.Completed);
        var cancelledCount = currentAppointments.Count(a => a.Status == AppointmentStatus.Cancelled);
        var noShowCount = currentAppointments.Count(a => a.Status == AppointmentStatus.NoShow);

        var completionRate = totalBookings > 0
            ? (decimal)completedCount / totalBookings * 100
            : 0;
        var cancellationRate = totalBookings > 0
            ? (decimal)cancelledCount / totalBookings * 100
            : 0;

        // Unique clients
        var uniqueClients = currentAppointments.Select(a => a.ClientId).Distinct().Count();
        var previousUniqueClients = previousAppointments.Select(a => a.ClientId).Distinct().Count();
        var clientsGrowth = previousUniqueClients > 0
            ? ((uniqueClients - previousUniqueClients) / (decimal)previousUniqueClients) * 100
            : 0;

        // Average booking value
        var avgBookingValue = completedCount > 0 ? totalRevenue / completedCount : 0;

        return Ok(new DashboardMetrics
        {
            TotalBookings = totalBookings,
            BookingsGrowth = Math.Round(bookingsGrowth, 1),
            TotalRevenue = totalRevenue,
            RevenueGrowth = Math.Round(revenueGrowth, 1),
            UniqueClients = uniqueClients,
            ClientsGrowth = Math.Round(clientsGrowth, 1),
            CompletionRate = Math.Round(completionRate, 1),
            CancellationRate = Math.Round(cancellationRate, 1),
            NoShowCount = noShowCount,
            AverageBookingValue = Math.Round(avgBookingValue, 2),
            PeriodDays = days
        });
    }

    /// <summary>
    /// Get revenue breakdown by service
    /// </summary>
    [HttpGet("revenue/by-service")]
    public async Task<ActionResult<List<ServiceRevenue>>> GetRevenueByService([FromQuery] int days = 30)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var startDate = DateTime.UtcNow.AddDays(-days);

        var revenueByService = await _context.Appointments
            .Where(a => a.TenantId == tenantId &&
                   a.CreatedAtUtc >= startDate &&
                   a.Status == AppointmentStatus.Completed)
            .GroupBy(a => new { a.ServiceId, a.Service.Name, a.Service.Color })
            .Select(g => new ServiceRevenue
            {
                ServiceId = g.Key.ServiceId,
                ServiceName = g.Key.Name,
                Color = g.Key.Color ?? "#666",
                TotalRevenue = g.Sum(a => a.Price),
                BookingCount = g.Count(),
                AveragePrice = g.Average(a => a.Price)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToListAsync();

        return Ok(revenueByService);
    }

    /// <summary>
    /// Get revenue breakdown by staff member
    /// </summary>
    [HttpGet("revenue/by-staff")]
    public async Task<ActionResult<List<StaffRevenue>>> GetRevenueByStaff([FromQuery] int days = 30)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var startDate = DateTime.UtcNow.AddDays(-days);

        var revenueByStaff = await _context.Appointments
            .Where(a => a.TenantId == tenantId &&
                   a.CreatedAtUtc >= startDate &&
                   a.Status == AppointmentStatus.Completed)
            .GroupBy(a => new { a.StaffMemberId, a.StaffMember.TenantUser.User.FirstName, a.StaffMember.TenantUser.User.LastName })
            .Select(g => new StaffRevenue
            {
                StaffId = g.Key.StaffMemberId,
                StaffName = g.Key.FirstName + " " + g.Key.LastName,
                TotalRevenue = g.Sum(a => a.Price),
                BookingCount = g.Count(),
                AveragePerBooking = g.Average(a => a.Price),
                TotalHours = g.Sum(a => a.DurationMinutes) / 60.0m
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToListAsync();

        return Ok(revenueByStaff);
    }

    /// <summary>
    /// Get daily booking trends
    /// </summary>
    [HttpGet("trends/bookings")]
    public async Task<ActionResult<List<DailyTrend>>> GetBookingTrends([FromQuery] int days = 30)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        var dailyData = await _context.Appointments
            .Where(a => a.TenantId == tenantId && a.CreatedAtUtc >= startDate)
            .GroupBy(a => a.CreatedAtUtc.Date)
            .Select(g => new
            {
                Date = g.Key,
                Bookings = g.Count(),
                Revenue = g.Where(a => a.Status == AppointmentStatus.Completed).Sum(a => a.Price),
                Completed = g.Count(a => a.Status == AppointmentStatus.Completed),
                Cancelled = g.Count(a => a.Status == AppointmentStatus.Cancelled)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Fill in missing days with zeros
        var result = new List<DailyTrend>();
        for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            var dayData = dailyData.FirstOrDefault(d => d.Date == date);
            result.Add(new DailyTrend
            {
                Date = date,
                Bookings = dayData?.Bookings ?? 0,
                Revenue = dayData?.Revenue ?? 0,
                Completed = dayData?.Completed ?? 0,
                Cancelled = dayData?.Cancelled ?? 0
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get peak hours analysis
    /// </summary>
    [HttpGet("insights/peak-hours")]
    public async Task<ActionResult<List<HourlyInsight>>> GetPeakHours([FromQuery] int days = 30)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var startDate = DateTime.UtcNow.AddDays(-days);

        var hourlyData = await _context.Appointments
            .Where(a => a.TenantId == tenantId && a.StartTimeUtc >= startDate)
            .GroupBy(a => a.StartTimeUtc.Hour)
            .Select(g => new HourlyInsight
            {
                Hour = g.Key,
                BookingCount = g.Count(),
                Revenue = g.Where(a => a.Status == AppointmentStatus.Completed).Sum(a => a.Price),
                AverageUtilization = 0 // Will calculate separately
            })
            .OrderBy(x => x.Hour)
            .ToListAsync();

        // Fill missing hours
        var result = Enumerable.Range(0, 24)
            .Select(h => hourlyData.FirstOrDefault(x => x.Hour == h) ?? new HourlyInsight { Hour = h })
            .ToList();

        // Calculate utilization (bookings / max possible for that hour)
        var maxBookings = result.Max(x => x.BookingCount);
        if (maxBookings > 0)
        {
            foreach (var hour in result)
            {
                hour.AverageUtilization = Math.Round((decimal)hour.BookingCount / maxBookings * 100, 1);
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Get client retention and loyalty metrics
    /// </summary>
    [HttpGet("insights/retention")]
    public async Task<ActionResult<RetentionMetrics>> GetRetentionMetrics([FromQuery] int days = 30)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var startDate = DateTime.UtcNow.AddDays(-days);
        var previousPeriodStart = startDate.AddDays(-days);

        // Get all client bookings
        var clientBookings = await _context.Appointments
            .Where(a => a.TenantId == tenantId)
            .GroupBy(a => a.ClientId)
            .Select(g => new
            {
                ClientId = g.Key,
                TotalBookings = g.Count(),
                FirstBooking = g.Min(a => a.CreatedAtUtc),
                LastBooking = g.Max(a => a.CreatedAtUtc),
                TotalSpent = g.Where(a => a.Status == AppointmentStatus.Completed).Sum(a => a.Price)
            })
            .ToListAsync();

        // New clients (first booking in current period)
        var newClients = clientBookings.Count(c => c.FirstBooking >= startDate);

        // Returning clients (had booking before current period AND in current period)
        var returningClients = clientBookings.Count(c =>
            c.FirstBooking < startDate && c.LastBooking >= startDate);

        // Clients at risk (no booking in current period but had one in previous)
        var atRiskClients = clientBookings.Count(c =>
            c.LastBooking < startDate && c.LastBooking >= previousPeriodStart);

        // Average bookings per client
        var avgBookingsPerClient = clientBookings.Count > 0
            ? clientBookings.Average(c => c.TotalBookings)
            : 0;

        // Customer lifetime value (average total spent)
        var avgLifetimeValue = clientBookings.Count > 0
            ? clientBookings.Average(c => c.TotalSpent)
            : 0;

        // Retention rate
        var previousPeriodClients = clientBookings.Count(c =>
            c.FirstBooking < startDate && c.LastBooking >= previousPeriodStart);
        var retentionRate = previousPeriodClients > 0
            ? (decimal)returningClients / previousPeriodClients * 100
            : 0;

        return Ok(new RetentionMetrics
        {
            NewClients = newClients,
            ReturningClients = returningClients,
            AtRiskClients = atRiskClients,
            TotalActiveClients = newClients + returningClients,
            RetentionRate = Math.Round(retentionRate, 1),
            AverageBookingsPerClient = Math.Round(avgBookingsPerClient, 1),
            AverageLifetimeValue = Math.Round(avgLifetimeValue, 2),
            PeriodDays = days
        });
    }

    /// <summary>
    /// Get actionable business recommendations
    /// </summary>
    [HttpGet("recommendations")]
    public async Task<ActionResult<List<Recommendation>>> GetRecommendations()
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return BadRequest("Tenant not specified");

        var recommendations = new List<Recommendation>();
        var last30Days = DateTime.UtcNow.AddDays(-30);

        // Check cancellation rate
        var appointments = await _context.Appointments
            .Where(a => a.TenantId == tenantId && a.CreatedAtUtc >= last30Days)
            .ToListAsync();

        if (appointments.Count > 10)
        {
            var cancellationRate = (decimal)appointments.Count(a => a.Status == AppointmentStatus.Cancelled) / appointments.Count * 100;
            if (cancellationRate > 15)
            {
                recommendations.Add(new Recommendation
                {
                    Type = "warning",
                    Title = "High Cancellation Rate",
                    Description = $"Your cancellation rate is {cancellationRate:F1}%. Consider implementing reminder notifications or requiring deposits.",
                    Impact = "Could recover up to 20% of lost revenue",
                    Priority = 1
                });
            }

            var noShowRate = (decimal)appointments.Count(a => a.Status == AppointmentStatus.NoShow) / appointments.Count * 100;
            if (noShowRate > 5)
            {
                recommendations.Add(new Recommendation
                {
                    Type = "warning",
                    Title = "No-Show Rate Alert",
                    Description = $"Your no-show rate is {noShowRate:F1}%. Send SMS reminders 24h and 2h before appointments.",
                    Impact = "Reduce no-shows by up to 50%",
                    Priority = 2
                });
            }
        }

        // Check for underutilized services
        var serviceBookings = await _context.Services
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .Select(s => new
            {
                s.Name,
                Bookings = s.Appointments.Count(a => a.CreatedAtUtc >= last30Days)
            })
            .ToListAsync();

        var avgBookings = serviceBookings.Average(s => s.Bookings);
        var underutilized = serviceBookings.Where(s => s.Bookings < avgBookings * 0.3).ToList();
        if (underutilized.Any())
        {
            recommendations.Add(new Recommendation
            {
                Type = "opportunity",
                Title = "Underperforming Services",
                Description = $"Services like '{underutilized.First().Name}' have low bookings. Consider promotions or bundling.",
                Impact = "Potential 15% revenue increase",
                Priority = 3
            });
        }

        // Check peak hour optimization
        var peakHours = await _context.Appointments
            .Where(a => a.TenantId == tenantId && a.StartTimeUtc >= last30Days)
            .GroupBy(a => a.StartTimeUtc.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(3)
            .ToListAsync();

        if (peakHours.Any())
        {
            recommendations.Add(new Recommendation
            {
                Type = "insight",
                Title = "Peak Hours Identified",
                Description = $"Your busiest hours are {string.Join(", ", peakHours.Select(p => $"{p.Hour}:00"))}. Consider premium pricing during these times.",
                Impact = "Potential 10% revenue increase",
                Priority = 4
            });
        }

        // Retention opportunity
        var returningClientsCount = await _context.Appointments
            .Where(a => a.TenantId == tenantId && a.CreatedAtUtc >= last30Days)
            .GroupBy(a => a.ClientId)
            .CountAsync(g => g.Count() > 1);

        var totalClients = await _context.Appointments
            .Where(a => a.TenantId == tenantId && a.CreatedAtUtc >= last30Days)
            .Select(a => a.ClientId)
            .Distinct()
            .CountAsync();

        if (totalClients > 0)
        {
            var repeatRate = (decimal)returningClientsCount / totalClients * 100;
            if (repeatRate < 30)
            {
                recommendations.Add(new Recommendation
                {
                    Type = "opportunity",
                    Title = "Improve Client Retention",
                    Description = $"Only {repeatRate:F0}% of clients rebook. Implement a loyalty program or follow-up emails.",
                    Impact = "5% retention increase = 25% profit increase",
                    Priority = 2
                });
            }
        }

        return Ok(recommendations.OrderBy(r => r.Priority).ToList());
    }
}

// DTOs
public class DashboardMetrics
{
    public int TotalBookings { get; set; }
    public decimal BookingsGrowth { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenueGrowth { get; set; }
    public int UniqueClients { get; set; }
    public decimal ClientsGrowth { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal CancellationRate { get; set; }
    public int NoShowCount { get; set; }
    public decimal AverageBookingValue { get; set; }
    public int PeriodDays { get; set; }
}

public class ServiceRevenue
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Color { get; set; } = "#666";
    public decimal TotalRevenue { get; set; }
    public int BookingCount { get; set; }
    public decimal AveragePrice { get; set; }
}

public class StaffRevenue
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int BookingCount { get; set; }
    public decimal AveragePerBooking { get; set; }
    public decimal TotalHours { get; set; }
}

public class DailyTrend
{
    public DateTime Date { get; set; }
    public int Bookings { get; set; }
    public decimal Revenue { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
}

public class HourlyInsight
{
    public int Hour { get; set; }
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageUtilization { get; set; }
}

public class RetentionMetrics
{
    public int NewClients { get; set; }
    public int ReturningClients { get; set; }
    public int AtRiskClients { get; set; }
    public int TotalActiveClients { get; set; }
    public decimal RetentionRate { get; set; }
    public double AverageBookingsPerClient { get; set; }
    public decimal AverageLifetimeValue { get; set; }
    public int PeriodDays { get; set; }
}

public class Recommendation
{
    public string Type { get; set; } = string.Empty; // warning, opportunity, insight
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public int Priority { get; set; }
}
