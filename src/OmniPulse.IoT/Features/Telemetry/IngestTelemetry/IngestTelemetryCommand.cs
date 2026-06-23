using MediatR;

namespace OmniPulse.IoT.Features.Telemetry.IngestTelemetry;

// Sahadaki cihazların fırlatacağı ham telemetri paketi kontratı
public record IngestTelemetryCommand(
    string DeviceId, 
    double Temperature, 
    double Pressure, 
    DateTime Timestamp) : IRequest<IngestTelemetryResponse>;

public record IngestTelemetryResponse(
    string Message,
    bool IsSuccess,
    DateTime ProcessedAt
);