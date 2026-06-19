using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;

// Primary Constructor ile tertemiz bir bağımlılık enjeksiyonu (Şimdilik servis yok ama şablon hazır!)
public class IngestTelemetryCommandHandler 
    : IRequestHandler<IngestTelemetryCommand, IngestTelemetryResponse>
{
    public Task<IngestTelemetryResponse> Handle(IngestTelemetryCommand request, CancellationToken cancellationToken)
    {
        // TODO: Burada ileride ITenantContext üzerinden aktif kiracıyı koklayıp 
        // IoTDbContext ile veriyi o kiracının veritabanına izole şekilde mühürleyeceğiz!

        var response = new IngestTelemetryResponse(
            Message: $"MomoYuki Telemetri Tüneli Aktif! Cihaz [{request.DeviceId}] verisi başarıyla alındı. ⚡",
            IsSuccess: true,
            ProcessedAt: DateTime.UtcNow
        );

        return Task.FromResult(response);
    }
}