using System;
using System.Collections.Generic;
using MediatR;

namespace OmniPulse.IoT.Features.Telemetry.GetTelemetryReport;

public record GetTelemetryReportQuery(
    Guid? DeviceId,
    Guid? AssetId,
    DateTime StartDate,
    DateTime EndDate,
    string MetricKey = "Temperature",
    double ColdChainThreshold = 60.0
) : IRequest<TelemetryReportDto>;

public record TelemetryReportDto(
    double MinValue,
    double MaxValue,
    double AverageValue,
    int TotalCount,
    int BreachCount,
    double BreachPercentage,
    List<BreachPointDto> BreachedPoints,
    string Message
);

public record BreachPointDto(
    Guid Id,
    Guid DeviceId,
    string DeviceName,
    string? AssetName,
    double Value,
    double Threshold,
    DateTime Timestamp
);
