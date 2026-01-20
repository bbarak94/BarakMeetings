namespace BookingPlatform.Domain.Interfaces;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    void SetTenant(Guid tenantId);
    void ClearTenant();
}
