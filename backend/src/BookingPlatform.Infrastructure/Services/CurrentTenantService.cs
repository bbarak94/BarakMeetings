using BookingPlatform.Domain.Interfaces;

namespace BookingPlatform.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    private Guid? _tenantId;

    public Guid? TenantId => _tenantId;

    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
    }

    public void ClearTenant()
    {
        _tenantId = null;
    }
}
