using System;
using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;

public record TelemetryIngestedEvent(
    Guid TenantId,
    Guid DeviceId,
    string DeviceName,
    double Temperature,
    double Pressure,
    DateTime Timestamp
) : INotification;
