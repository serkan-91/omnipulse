using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.IoT.Features.Alarms.GetAlarmRules;

public static class GetAlarmRulesEndpoint
{
    public static IEndpointRouteBuilder MapGetAlarmRulesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/iot/alarms/rules", async (ISender mediator) =>
            {
                var response = await mediator.Send(new GetAlarmRulesQuery());
                return Results.Ok(response);
            })
            .WithName("GetAlarmRules")
            .WithTags("IoT Alarm Rules")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}
