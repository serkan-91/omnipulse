using MediatR;

namespace OmniPulse.Tenant.Features.Tenants.CheckTenant;

// Dikey Dilimimizin İstek Şeması 🍕
public record CheckTenantCommand : IRequest<CheckTenantResponse>;

public record CheckTenantResponse(
    string Message,
    string DetectedTenant,
    string TenantName,
    string ActiveConnectionString
);
