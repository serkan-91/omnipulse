using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.IoT.Features.DeviceCategories.CreateCategory;

public static class CreateCategoryEndpoint
{
    public static IEndpointRouteBuilder MapCreateCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/categories", async (CreateCategoryCommand command, ISender mediator) =>
            {
                var response = await mediator.Send(command);
                return Results.Created($"api/iot/categories/{response.Id}", response);
            })
            .WithName("CreateDeviceCategory")
            .WithTags("IoT Device Categories")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}
