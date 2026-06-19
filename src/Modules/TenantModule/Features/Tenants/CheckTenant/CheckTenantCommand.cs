using MediatR;

namespace OmniPulse.Modules.TenantModule.Features.Tenants.CheckTenant;

// Dikey Dilimimizin İstek Şeması 🍕
public record CheckTenantCommand : IRequest<CheckTenantResponse>;

public record CheckTenantResponse(
    string Message,
    string DetectedTenant,
    string TenantName,
    string ActiveConnectionString
);
