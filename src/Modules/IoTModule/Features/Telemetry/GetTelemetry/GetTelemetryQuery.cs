using System;
using System.Collections.Generic;
using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry.GetTelemetry;

public record GetTelemetryQuery(
    Guid? DeviceId = null,
    Guid? AssetId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<TelemetryDto>>;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record TelemetryDto(
    Guid Id,
    Guid DeviceId,
    string DeviceName,
    string DeviceSerialNumber,
    Guid? AssetId,
    string? AssetName,
    double Temperature,
    double Pressure,
    DateTime Timestamp
);
