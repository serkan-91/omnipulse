using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.IoT.Features.Alarms.GetActiveAlarms;

public static class GetActiveAlarmsEndpoint
{
    public static IEndpointRouteBuilder MapGetActiveAlarmsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/iot/alarms/active", async (
                Guid? categoryId,
                Guid? assetId,
                ISender mediator) =>
            {
                var response = await mediator.Send(new GetActiveAlarmsQuery(categoryId, assetId));
                return Results.Ok(response);
            })
            .WithName("GetActiveAlarms")
            .WithTags("IoT Alarms")
            .RequireAuthorization(); // JWT Authentication required 🔐

        return app;
    }
}
