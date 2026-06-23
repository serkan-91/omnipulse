using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.IoT.Features.Telemetry.ReportDeviceStatus;

public static class ReportDeviceStatusEndpoint
{
    public static IEndpointRouteBuilder MapReportDeviceStatusEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/devices/status", async (ReportDeviceStatusCommand command, ISender mediator) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName("ReportDeviceConnectionStatus")
            .WithTags("IoT Telemetry");

        return app;
    }
}
