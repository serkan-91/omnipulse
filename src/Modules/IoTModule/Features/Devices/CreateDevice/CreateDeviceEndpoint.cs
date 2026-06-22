using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.IoTModule.Features.Devices.CreateDevice;

public static class CreateDeviceEndpoint
{
    public static IEndpointRouteBuilder MapCreateDeviceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/devices", async (CreateDeviceCommand command, ISender mediator) =>
            {
                var response = await mediator.Send(command);
                return Results.Created($"api/iot/devices/{response.Id}", response);
            })
            .WithName("CreateDevice")
            .WithTags("IoT Devices")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}
