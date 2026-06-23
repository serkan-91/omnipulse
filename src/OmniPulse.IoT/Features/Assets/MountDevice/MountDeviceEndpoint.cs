using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.IoT.Features.Assets.MountDevice;

public static class MountDeviceEndpoint
{
    public static IEndpointRouteBuilder MapMountDeviceToAssetEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/assets/{assetId:guid}/devices/{deviceId:guid}/mount",
            async (Guid assetId, Guid deviceId, ISender mediator) =>
            {
                var response = await mediator.Send(new MountDeviceCommand(deviceId, assetId));
                return Results.Ok(response);
            })
            .WithName("MountDeviceToAsset")
            .WithTags("IoT Assets")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}
