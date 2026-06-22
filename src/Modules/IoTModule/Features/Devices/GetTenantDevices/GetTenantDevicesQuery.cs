using MediatR;
using System;
using System.Collections.Generic;

namespace OmniPulse.Modules.IoTModule.Features.Devices.GetTenantDevices;

public record GetTenantDevicesQuery : IRequest<IEnumerable<TenantDeviceDto>>;

public record TenantDeviceDto(
    Guid Id,
    string Name,
    string SerialNumber,
    bool IsActive,
    Guid? AssetId,
    string? AssetName,
    Guid? CategoryId,
    string? CategoryName
);
