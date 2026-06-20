using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.IoTModule.Features.Alarms.CreateAlarmRule;

public static class CreateAlarmRuleEndpoint
{
    public static IEndpointRouteBuilder MapCreateAlarmRuleEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/alarms/rules", async (CreateAlarmRuleCommand command, ISender mediator) =>
            {
                var response = await mediator.Send(command);
                return Results.Created($"api/iot/alarms/rules/{response.Id}", response);
            })
            .WithName("CreateAlarmRule")
            .WithTags("IoT Alarm Rules")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}
