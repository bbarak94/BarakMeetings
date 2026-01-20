using BookingPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookingPlatform.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=BookingPlatform_Dev;Username=postgres;Password=postgres");

        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeTenantService());
    }

    private class DesignTimeTenantService : ICurrentTenantService
    {
        public Guid? TenantId => null;
        public void SetTenant(Guid tenantId) { }
        public void ClearTenant() { }
    }
}
