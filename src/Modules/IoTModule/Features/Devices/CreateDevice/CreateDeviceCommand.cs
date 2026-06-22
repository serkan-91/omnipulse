using System;
using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Devices.CreateDevice;

public record CreateDeviceCommand(
    string Name,
    string SerialNumber,
    Guid? CategoryId = null,
    Guid? AssetId = null
) : IRequest<CreateDeviceResponse>;

public record CreateDeviceResponse(
    Guid Id,
    string Name,
    string SerialNumber,
    Guid? CategoryId,
    Guid? AssetId,
    string Message
);
