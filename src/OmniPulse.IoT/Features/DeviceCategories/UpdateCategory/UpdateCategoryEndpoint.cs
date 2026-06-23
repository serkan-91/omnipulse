using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.IoT.Features.DeviceCategories.UpdateCategory;

public static class UpdateCategoryEndpoint
{
    public static IEndpointRouteBuilder MapUpdateCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("api/iot/categories/{id:guid}", async (Guid id, UpdateCategoryCommand command, ISender mediator) =>
            {
                if (id != command.Id)
                {
                    return Results.BadRequest("İstek gövdesindeki ID ile URL'deki ID uyuşmuyor şefim!");
                }

                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName("UpdateDeviceCategory")
            .WithTags("IoT Device Categories")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}
