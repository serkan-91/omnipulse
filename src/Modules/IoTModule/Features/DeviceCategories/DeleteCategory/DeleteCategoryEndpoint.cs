using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.IoTModule.Features.DeviceCategories.DeleteCategory;

public static class DeleteCategoryEndpoint
{
    public static IEndpointRouteBuilder MapDeleteCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("api/iot/categories/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new DeleteCategoryCommand(id));
                return Results.Ok(response);
            })
            .WithName("DeleteDeviceCategory")
            .WithTags("IoT Device Categories")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}
