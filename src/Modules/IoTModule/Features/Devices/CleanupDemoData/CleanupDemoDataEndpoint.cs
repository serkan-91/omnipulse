using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MediatR;
using System.Threading.Tasks;

namespace OmniPulse.Modules.IoTModule.Features.Devices.CleanupDemoData;

public static class CleanupDemoDataEndpoint
{
    public static IEndpointRouteBuilder MapCleanupDemoDataEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/devices/cleanup-demo", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new CleanupDemoDataCommand());
            return result.IsSuccess 
                ? Results.Ok(result) 
                : Results.BadRequest(result);
        })
        .WithName("CleanupDemoData")
        .WithTags("IoT Module")
        .RequireAuthorization();

        return app;
    }
}
