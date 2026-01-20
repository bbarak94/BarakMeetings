using BookingPlatform.Domain.Entities;
using BookingPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BookingPlatform.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrated successfully");

            if (await context.Tenants.AnyAsync())
            {
                logger.LogInformation("Database already seeded");
                return;
            }

            await SeedDataAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private static async Task SeedDataAsync(ApplicationDbContext context, ILogger logger)
    {
        // Create Demo Tenant
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "FitStudio Demo",
            Slug = "fitstudio",
            Description = "A demo fitness studio for testing the platform",
            Email = "info@fitstudio.demo",
            Phone = "+1-555-000-0000",
            Address = "123 Fitness Ave",
            City = "New York",
            Country = "USA",
            TimeZone = "America/New_York",
            Currency = "USD",
            Template = BusinessTemplate.FitnessStudio,
            Plan = SubscriptionPlan.Professional,
            IsActive = true
        };
        context.Tenants.Add(tenant);

        // Create Demo Admin User (password: Demo123!)
        var adminId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var adminUser = new ApplicationUser
        {
            Id = adminId,
            Email = "admin@fitstudio.demo",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
            FirstName = "Admin",
            LastName = "User",
            IsActive = true,
            EmailConfirmed = true
        };
        context.Users.Add(adminUser);

        // Create Staff Users
        var staff1UserId = Guid.Parse("33333333-3333-3333-3333-333333333331");
        var staff1User = new ApplicationUser
        {
            Id = staff1UserId,
            Email = "sarah@fitstudio.demo",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
            FirstName = "Sarah",
            LastName = "Johnson",
            IsActive = true,
            EmailConfirmed = true
        };
        context.Users.Add(staff1User);

        var staff2UserId = Guid.Parse("33333333-3333-3333-3333-333333333332");
        var staff2User = new ApplicationUser
        {
            Id = staff2UserId,
            Email = "mike@fitstudio.demo",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
            FirstName = "Mike",
            LastName = "Thompson",
            IsActive = true,
            EmailConfirmed = true
        };
        context.Users.Add(staff2User);

        var staff3UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var staff3User = new ApplicationUser
        {
            Id = staff3UserId,
            Email = "emily@fitstudio.demo",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
            FirstName = "Emily",
            LastName = "Chen",
            IsActive = true,
            EmailConfirmed = true
        };
        context.Users.Add(staff3User);

        // Create Client User (password: Demo123!)
        var clientUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var clientUser = new ApplicationUser
        {
            Id = clientUserId,
            Email = "client@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            EmailConfirmed = true
        };
        context.Users.Add(clientUser);

        // Link Admin to Tenant
        var tenantAdminId = Guid.Parse("55555555-5555-5555-5555-555555555551");
        var tenantAdmin = new TenantUser
        {
            Id = tenantAdminId,
            TenantId = tenantId,
            UserId = adminId,
            Role = TenantRole.Owner,
            IsActive = true
        };
        context.TenantUsers.Add(tenantAdmin);

        // Link Staff to Tenant
        var tenantStaff1Id = Guid.Parse("55555555-5555-5555-5555-555555555552");
        var tenantStaff1 = new TenantUser
        {
            Id = tenantStaff1Id,
            TenantId = tenantId,
            UserId = staff1UserId,
            Role = TenantRole.Staff,
            IsActive = true
        };
        context.TenantUsers.Add(tenantStaff1);

        var tenantStaff2Id = Guid.Parse("55555555-5555-5555-5555-555555555553");
        var tenantStaff2 = new TenantUser
        {
            Id = tenantStaff2Id,
            TenantId = tenantId,
            UserId = staff2UserId,
            Role = TenantRole.Staff,
            IsActive = true
        };
        context.TenantUsers.Add(tenantStaff2);

        var tenantStaff3Id = Guid.Parse("55555555-5555-5555-5555-555555555554");
        var tenantStaff3 = new TenantUser
        {
            Id = tenantStaff3Id,
            TenantId = tenantId,
            UserId = staff3UserId,
            Role = TenantRole.Staff,
            IsActive = true
        };
        context.TenantUsers.Add(tenantStaff3);

        // Create Staff Members
        var staffMember1Id = Guid.Parse("66666666-6666-6666-6666-666666666661");
        var staffMember1 = new StaffMember
        {
            Id = staffMember1Id,
            TenantId = tenantId,
            TenantUserId = tenantStaff1Id,
            Title = "Yoga Instructor",
            Bio = "Certified yoga instructor with 10 years experience",
            AcceptsBookings = true,
            IsActive = true,
            SortOrder = 1
        };
        context.StaffMembers.Add(staffMember1);

        var staffMember2Id = Guid.Parse("66666666-6666-6666-6666-666666666662");
        var staffMember2 = new StaffMember
        {
            Id = staffMember2Id,
            TenantId = tenantId,
            TenantUserId = tenantStaff2Id,
            Title = "Personal Trainer",
            Bio = "Personal trainer and nutrition specialist",
            AcceptsBookings = true,
            IsActive = true,
            SortOrder = 2
        };
        context.StaffMembers.Add(staffMember2);

        var staffMember3Id = Guid.Parse("66666666-6666-6666-6666-666666666663");
        var staffMember3 = new StaffMember
        {
            Id = staffMember3Id,
            TenantId = tenantId,
            TenantUserId = tenantStaff3Id,
            Title = "Massage Therapist",
            Bio = "Licensed massage therapist specializing in sports massage",
            AcceptsBookings = true,
            IsActive = true,
            SortOrder = 3
        };
        context.StaffMembers.Add(staffMember3);

        // Create Client Profile
        var clientId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var client = new Client
        {
            Id = clientId,
            TenantId = tenantId,
            UserId = clientUserId,
            FirstName = "John",
            LastName = "Doe",
            Email = "client@example.com",
            PhoneNumber = "+1-555-123-4567",
            Notes = "VIP member - prefers morning sessions",
            AllowMarketing = true,
            IsActive = true
        };
        context.Clients.Add(client);

        // Create Services
        var yogaServiceId = Guid.Parse("88888888-8888-8888-8888-888888888881");
        var yogaService = new ServiceDefinition
        {
            Id = yogaServiceId,
            TenantId = tenantId,
            Name = "Yoga Class",
            Description = "Relaxing yoga session for all levels. Includes meditation and breathing exercises.",
            BaseDurationMinutes = 60,
            BasePrice = 25.00m,
            Capacity = 15,
            BufferMinutes = 10,
            Color = "#4CAF50",
            IsActive = true,
            SortOrder = 1
        };
        context.Services.Add(yogaService);

        var ptServiceId = Guid.Parse("88888888-8888-8888-8888-888888888882");
        var ptService = new ServiceDefinition
        {
            Id = ptServiceId,
            TenantId = tenantId,
            Name = "Personal Training",
            Description = "1-on-1 personal training session tailored to your fitness goals.",
            BaseDurationMinutes = 45,
            BasePrice = 75.00m,
            Capacity = 1,
            BufferMinutes = 15,
            Color = "#2196F3",
            IsActive = true,
            SortOrder = 2
        };
        context.Services.Add(ptService);

        var massageServiceId = Guid.Parse("88888888-8888-8888-8888-888888888883");
        var massageService = new ServiceDefinition
        {
            Id = massageServiceId,
            TenantId = tenantId,
            Name = "Sports Massage",
            Description = "Deep tissue sports massage for recovery and relaxation.",
            BaseDurationMinutes = 60,
            BasePrice = 95.00m,
            Capacity = 1,
            BufferMinutes = 15,
            Color = "#9C27B0",
            IsActive = true,
            SortOrder = 3
        };
        context.Services.Add(massageService);

        var spinServiceId = Guid.Parse("88888888-8888-8888-8888-888888888884");
        var spinService = new ServiceDefinition
        {
            Id = spinServiceId,
            TenantId = tenantId,
            Name = "Spin Class",
            Description = "High-intensity indoor cycling for cardio lovers.",
            BaseDurationMinutes = 45,
            BasePrice = 20.00m,
            Capacity = 20,
            BufferMinutes = 10,
            Color = "#FF5722",
            IsActive = true,
            SortOrder = 4
        };
        context.Services.Add(spinService);

        // Link Staff to Services
        context.StaffServiceLinks.AddRange(new[]
        {
            new StaffServiceLink { Id = Guid.NewGuid(), TenantId = tenantId, StaffMemberId = staffMember1Id, ServiceId = yogaServiceId },
            new StaffServiceLink { Id = Guid.NewGuid(), TenantId = tenantId, StaffMemberId = staffMember1Id, ServiceId = spinServiceId },
            new StaffServiceLink { Id = Guid.NewGuid(), TenantId = tenantId, StaffMemberId = staffMember2Id, ServiceId = ptServiceId },
            new StaffServiceLink { Id = Guid.NewGuid(), TenantId = tenantId, StaffMemberId = staffMember2Id, ServiceId = spinServiceId },
            new StaffServiceLink { Id = Guid.NewGuid(), TenantId = tenantId, StaffMemberId = staffMember3Id, ServiceId = massageServiceId }
        });

        // Create Working Hours (Mon-Fri 8am-6pm, Sat 9am-2pm)
        var workingHours = new List<WorkingHours>();
        foreach (var staffId in new[] { staffMember1Id, staffMember2Id, staffMember3Id })
        {
            for (int day = 1; day <= 5; day++) // Mon-Fri
            {
                workingHours.Add(new WorkingHours
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    StaffMemberId = staffId,
                    DayOfWeek = (DayOfWeek)day,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(18, 0),
                    IsActive = true
                });
            }
            // Saturday
            workingHours.Add(new WorkingHours
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StaffMemberId = staffId,
                DayOfWeek = DayOfWeek.Saturday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(14, 0),
                IsActive = true
            });
        }
        context.WorkingHours.AddRange(workingHours);

        // Create Sample Appointments
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var appointment1 = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = yogaServiceId,
            StaffMemberId = staffMember1Id,
            ClientId = clientId,
            StartTimeUtc = tomorrow.AddHours(9),
            EndTimeUtc = tomorrow.AddHours(10),
            Status = AppointmentStatus.Confirmed,
            Price = 25.00m,
            DurationMinutes = 60,
            CustomerNotes = "First time in yoga class"
        };
        context.Appointments.Add(appointment1);

        var appointment2 = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = ptServiceId,
            StaffMemberId = staffMember2Id,
            ClientId = clientId,
            StartTimeUtc = tomorrow.AddHours(14),
            EndTimeUtc = tomorrow.AddHours(14).AddMinutes(45),
            Status = AppointmentStatus.Confirmed,
            Price = 75.00m,
            DurationMinutes = 45,
            CustomerNotes = "Focus on upper body"
        };
        context.Appointments.Add(appointment2);

        var appointment3 = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = massageServiceId,
            StaffMemberId = staffMember3Id,
            ClientId = clientId,
            StartTimeUtc = today.AddDays(2).AddHours(11),
            EndTimeUtc = today.AddDays(2).AddHours(12),
            Status = AppointmentStatus.Pending,
            Price = 95.00m,
            DurationMinutes = 60
        };
        context.Appointments.Add(appointment3);

        await context.SaveChangesAsync();
        logger.LogInformation("Database seeded successfully with demo data");
    }
}
