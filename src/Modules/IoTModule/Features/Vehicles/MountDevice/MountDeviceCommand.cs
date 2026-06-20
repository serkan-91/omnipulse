using System;
using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.MountDevice;

public record MountDeviceCommand(
    Guid DeviceId,
    Guid? VehicleId
) : IRequest<MountDeviceResponse>;

public record MountDeviceResponse(
    Guid DeviceId,
    Guid? VehicleId,
    string Message
);
