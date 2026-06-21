using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Assets.AssignResponsibleUser;

public record AssignResponsibleUserCommand(
    Guid AssetId,
    Guid UserId,
    string Role,
    bool IsUnassign = false
) : IRequest<AssignResponsibleUserResponse>;

public record AssignResponsibleUserResponse(
    Guid AssetId,
    string AssetName,
    Guid UserId,
    string Role,
    string Status
);
