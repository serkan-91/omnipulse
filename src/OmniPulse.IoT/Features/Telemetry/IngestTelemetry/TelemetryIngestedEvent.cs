using System;
using MediatR;

namespace OmniPulse.IoT.Features.Telemetry.IngestTelemetry;

public record TelemetryIngestedEvent(
    Guid TenantId,
    Guid DeviceId,
    string DeviceName,
    double Temperature,
    double Pressure,
    DateTime Timestamp,
    string? TraceId = null
) : INotification;
