using MediatR;

namespace OmniPulse.Modules.TenantModule.Features.Tenants.AcceptInvite;

public record AcceptInviteCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password
) : IRequest<AcceptInviteResponse>;

public record AcceptInviteResponse(
    bool IsSuccess,
    string? Token,
    string? Message
);
