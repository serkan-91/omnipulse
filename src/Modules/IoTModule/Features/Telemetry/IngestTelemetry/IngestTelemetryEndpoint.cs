using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;

public static class IngestTelemetryEndpoint
{
    public static IEndpointRouteBuilder MapIngestTelemetryEndpoint(this IEndpointRouteBuilder app)
    {
        // İşte buraya eklediğimiz an .NET bu nesneyi HTTP'den besleyeceğini anlar, Rider da susar!
        app.MapPost("api/iot/telemetry", async (IngestTelemetryCommand command, ISender mediator) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName("IngestDeviceTelemetry")
            .WithTags("IoT Telemetry");

        return app;
    }
}