using BookingPlatform.Application.Services;
using BookingPlatform.Domain.Interfaces;
using BookingPlatform.Infrastructure.Persistence;
using BookingPlatform.Infrastructure.Persistence.Repositories;
using BookingPlatform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register CurrentTenantService as Scoped (per-request)
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();

        // Register DbContext
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                });
        });

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register JWT service
        services.AddScoped<IJwtService, JwtService>();

        // Register Email service
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
