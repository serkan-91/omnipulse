using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.AssignDriver;

public static class AssignDriverEndpoint
{
    public static IEndpointRouteBuilder MapAssignDriverEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/vehicles/{id:guid}/assign-driver", async (Guid id, AssignDriverDto dto, ISender mediator) =>
            {
                var response = await mediator.Send(new AssignDriverCommand(id, dto.DriverUserId));
                return Results.Ok(response);
            })
            .WithName("AssignDriverToVehicle")
            .WithTags("IoT Vehicles")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}

public record AssignDriverDto(Guid? DriverUserId);
