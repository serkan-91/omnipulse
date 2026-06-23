using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MediatR;
using System.Threading.Tasks;

namespace OmniPulse.IoT.Features.Devices.SeedDemoData;

public static class SeedDemoDataEndpoint
{
    public static IEndpointRouteBuilder MapSeedDemoDataEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/devices/seed-demo", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new SeedDemoDataCommand());
            return result.IsSuccess 
                ? Results.Ok(result) 
                : Results.BadRequest(result);
        })
        .WithName("SeedDemoData")
        .WithTags("IoT Module")
        .RequireAuthorization();

        return app;
    }
}
