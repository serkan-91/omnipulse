using OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;

namespace OmniPulse.Identity.API.Endpoints;

public static class IoTEndpoints
{
    public static IEndpointRouteBuilder MapIoTEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapIngestTelemetryEndpoint();
        return app;
    }
}
