using MediatR;

namespace OmniPulse.IoT.Features.Assets.MountDevice;

public record MountDeviceCommand(
    Guid DeviceId,
    Guid? AssetId
) : IRequest<MountDeviceResponse>;

public record MountDeviceResponse(
    Guid DeviceId,
    Guid? AssetId,
    string Message
);
