using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Assets.MountDevice;

public record MountDeviceCommand(
    Guid DeviceId,
    Guid? AssetId
) : IRequest<MountDeviceResponse>;

public record MountDeviceResponse(
    Guid DeviceId,
    Guid? AssetId,
    string Message
);
