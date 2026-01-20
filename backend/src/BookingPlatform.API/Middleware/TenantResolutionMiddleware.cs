using System.Security.Claims;
using BookingPlatform.Domain.Interfaces;

namespace BookingPlatform.API.Middleware;

public class TenantResolutionMiddleware
{
    private const string TenantIdHeader = "X-Tenant-Id";
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService)
    {
        // Try to get TenantId from header first
        if (context.Request.Headers.TryGetValue(TenantIdHeader, out var tenantIdHeader)
            && Guid.TryParse(tenantIdHeader, out var headerTenantId))
        {
            tenantService.SetTenant(headerTenantId);
        }
        // Then try JWT claim
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenantId");
            if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var claimTenantId))
            {
                tenantService.SetTenant(claimTenantId);
            }
        }

        await _next(context);
    }
}
