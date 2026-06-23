using System;
using MediatR;

namespace OmniPulse.IoT.Features.Telemetry.ReportDeviceStatus;

public record ReportDeviceStatusCommand(
    string DeviceId,
    bool IsOnline,
    DateTime Timestamp) : IRequest<ReportDeviceStatusResponse>;

public record ReportDeviceStatusResponse(
    string Message,
    bool IsSuccess,
    DateTime ProcessedAt
);
