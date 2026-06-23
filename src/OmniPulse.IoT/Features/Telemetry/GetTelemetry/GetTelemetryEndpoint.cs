using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.IoT.Features.Telemetry.GetTelemetry;

public static class GetTelemetryEndpoint
{
    public static IEndpointRouteBuilder MapGetTelemetryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/iot/telemetry", async (
                [FromQuery] Guid? deviceId,
                [FromQuery] Guid? assetId,
                [FromQuery] DateTime? startDate,
                [FromQuery] DateTime? endDate,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                ISender mediator) =>
            {
                var query = new GetTelemetryQuery(
                    DeviceId: deviceId,
                    AssetId: assetId,
                    StartDate: startDate,
                    EndDate: endDate,
                    Page: page > 0 ? page : 1,
                    PageSize: pageSize > 0 ? pageSize : 50
                );

                var response = await mediator.Send(query);
                return Results.Ok(response);
            })
            .WithName("GetTelemetry")
            .WithTags("IoT Telemetry")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}
