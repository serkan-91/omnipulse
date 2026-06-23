using MediatR;

namespace OmniPulse.Tenant.Features.Tenants.RegisterTenant;

public record RegisterTenantCommand(
    string CompanyName,
    string TenantIdentifier, // Örn: "pandaholding"
    string FirstName,
    string LastName,
    string Email,
    string Password
) : IRequest<RegisterTenantResponse>;

public record RegisterTenantResponse(
    bool IsSuccess,
    string? Token,
    string? Message
);
