using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.MountDevice;

public static class MountDeviceEndpoint
{
    public static IEndpointRouteBuilder MapMountDeviceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/devices/{id:guid}/mount", async (Guid id, MountDeviceDto dto, ISender mediator) =>
            {
                var response = await mediator.Send(new MountDeviceCommand(id, dto.VehicleId));
                return Results.Ok(response);
            })
            .WithName("MountDeviceToVehicle")
            .WithTags("IoT Devices")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}

public record MountDeviceDto(Guid? VehicleId);
