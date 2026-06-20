using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.IoTModule.Features.Vehicles.GetVehicles;

public static class GetVehiclesEndpoint
{
    public static IEndpointRouteBuilder MapGetVehiclesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/iot/vehicles", async (
                [FromQuery] string? searchTerm,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                ISender mediator) =>
            {
                var query = new GetVehiclesQuery(
                    SearchTerm: searchTerm,
                    Page: page > 0 ? page : 1,
                    PageSize: pageSize > 0 ? pageSize : 50
                );

                var response = await mediator.Send(query);
                return Results.Ok(response);
            })
            .WithName("GetVehicles")
            .WithTags("IoT Vehicles")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}
