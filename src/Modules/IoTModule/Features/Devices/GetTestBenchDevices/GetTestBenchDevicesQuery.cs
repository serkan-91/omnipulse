using System;
using System.Collections.Generic;
using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Devices.GetTestBenchDevices;

public record GetTestBenchDevicesQuery() : IRequest<IEnumerable<TestBenchDeviceItem>>;

public record TestBenchDeviceItem(
    Guid Id,
    string Name,
    string SerialNumber,
    Guid? CategoryId,
    string? CategoryName,
    bool IsActive,
    DateTime CreatedAtUtc
);
