using Microsoft.AspNetCore.Http;
using OmniPulse.Tenant.Features.Common.Interfaces;
using OmniPulse.Tenant.Domain.Entities;

namespace OmniPulse.Tenant.Infrastructure.MultiTenancy;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private OmniPulse.Tenant.Domain.Entities.Tenant? _currentTenant;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        DetermineCurrentTenant();
    }

    public string? GetCurrentTenantIdentifier() => _currentTenant?.Identifier;

    public string? GetCurrentConnectionString() => _currentTenant?.ConnectionString;

    public OmniPulse.Tenant.Domain.Entities.Tenant? GetCurrentTenant() => _currentTenant;

    public void SetTenant(OmniPulse.Tenant.Domain.Entities.Tenant tenant) => _currentTenant = tenant;

    private void DetermineCurrentTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // 🎯 BAŞLIĞI DÜZELTTİK: Testlerde atacağımız "X-Tenant-Id" ile jilet gibi eşitledik sevgilim! 🏎️💨
        if (!httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader)) return;
        
        var identifier = tenantHeader.ToString().ToLowerInvariant().Trim();
        
        if (identifier == "pandaberry")
        {
            _currentTenant = OmniPulse.Tenant.Domain.Entities.Tenant.Create(
                "PandaBerry Gaming", 
                "pandaberry", 
                "Host=localhost;Port=5432;Database=omnipulse_identity_pandaberry;Username=omnipulse_admin;Password=SuperSecurePassword123!;", 
                System.DateTime.UtcNow.AddYears(1));
        }
    }
}
