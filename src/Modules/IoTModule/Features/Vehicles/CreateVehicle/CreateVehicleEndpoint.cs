using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.CreateVehicle;

public static class CreateVehicleEndpoint
{
    public static IEndpointRouteBuilder MapCreateVehicleEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/vehicles", async (CreateVehicleCommand command, ISender mediator) =>
            {
                var response = await mediator.Send(command);
                return Results.Created($"api/iot/vehicles/{response.Id}", response);
            })
            .WithName("CreateVehicle")
            .WithTags("IoT Vehicles")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}
