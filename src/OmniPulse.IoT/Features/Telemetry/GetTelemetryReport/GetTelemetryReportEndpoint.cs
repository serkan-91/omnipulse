using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.IoT.Features.Telemetry.GetTelemetryReport;

public static class GetTelemetryReportEndpoint
{
    public static IEndpointRouteBuilder MapGetTelemetryReportEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/iot/reports/cold-chain", async (
                [FromQuery] Guid? deviceId,
                [FromQuery] Guid? assetId,
                [FromQuery] DateTime? startDate,
                [FromQuery] DateTime? endDate,
                [FromQuery] string? metricKey,
                [FromQuery] double? coldChainThreshold,
                ISender mediator) =>
            {
                var query = new GetTelemetryReportQuery(
                    DeviceId: deviceId,
                    AssetId: assetId,
                    StartDate: startDate ?? DateTime.UtcNow.AddDays(-30),
                    EndDate: endDate ?? DateTime.UtcNow,
                    MetricKey: metricKey ?? "Temperature",
                    ColdChainThreshold: coldChainThreshold ?? 60.0
                );

                var response = await mediator.Send(query);
                return Results.Ok(response);
            })
            .WithName("GetColdChainReport")
            .WithTags("IoT Reports")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}
