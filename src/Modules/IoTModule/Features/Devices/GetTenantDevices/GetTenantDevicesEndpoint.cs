using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MediatR;
using System.Threading.Tasks;

namespace OmniPulse.Modules.IoTModule.Features.Devices.GetTenantDevices;

public static class GetTenantDevicesEndpoint
{
    public static IEndpointRouteBuilder MapGetTenantDevicesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/iot/devices", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetTenantDevicesQuery());
            return Results.Ok(result);
        })
        .WithName("GetTenantDevices")
        .WithTags("IoT Module")
        .RequireAuthorization();

        return app;
    }
}
