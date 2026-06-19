using MediatR;
using OmniPulse.Modules.TenantModule.Features.Common.Interfaces;

namespace OmniPulse.Modules.TenantModule.Features.Tenants.CheckTenant;

// Dikey Dilimimizin İş Mantığı (Handler) 🍕
public class CheckTenantCommandHandler(ITenantService tenantService) 
    : IRequestHandler<CheckTenantCommand, CheckTenantResponse>
{
    public Task<CheckTenantResponse> Handle(CheckTenantCommand request, CancellationToken cancellationToken)
    {
        var tenantIdentifier = tenantService.GetCurrentTenantIdentifier();
        var currentTenant = tenantService.GetCurrentTenant();
        var connectionString = tenantService.GetCurrentConnectionString();

        var response = new CheckTenantResponse(
            Message: "MomoYuki Modüler Minimal API Tüneli Aktif! 🏎️💨",
            DetectedTenant: tenantIdentifier ?? "Shared (Ortak Veritabanı)",
            TenantName: currentTenant?.Name ?? "Merkez Yönetim",
            ActiveConnectionString: connectionString ?? "Varsayılan Ortak Bağlantı Cümlesi"
        );

        return Task.FromResult(response);
    }
}
