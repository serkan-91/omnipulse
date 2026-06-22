using MediatR;

namespace OmniPulse.Modules.TenantModule.Features.Auth.Logout;

public record LogoutCommand(
    string Jti, 
    long ExpirySeconds,
    string? UserId,
    string? Email,
    string? TenantIdentifier,
    string? IpAddress,
    string? UserAgent
) : IRequest<LogoutResponse>;

public record LogoutResponse(bool IsSuccess, string Message);
