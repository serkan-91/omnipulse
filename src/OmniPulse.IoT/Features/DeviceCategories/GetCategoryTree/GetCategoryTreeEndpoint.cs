using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.IoT.Features.DeviceCategories.GetCategoryTree;

public static class GetCategoryTreeEndpoint
{
    public static IEndpointRouteBuilder MapGetCategoryTreeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/iot/categories/tree", async (ISender mediator) =>
            {
                var response = await mediator.Send(new GetCategoryTreeQuery());
                return Results.Ok(response);
            })
            .WithName("GetDeviceCategoryTree")
            .WithTags("IoT Device Categories")
            .RequireAuthorization(); // Kiracı bağlamı için JWT gereklidir 🔐

        return app;
    }
}
