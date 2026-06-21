using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Assets.CreateAsset;

public record CreateAssetCommand(
    string Type,
    string Name,
    Guid? ParentAssetId = null,
    Guid? ResponsibleUserId = null,
    string? MetadataJson = null
) : IRequest<CreateAssetResponse>;

public record CreateAssetResponse(
    Guid Id,
    string Name,
    string Type,
    Guid? ParentAssetId,
    Guid? ResponsibleUserId,
    string? MetadataJson,
    DateTime CreatedAtUtc
);
