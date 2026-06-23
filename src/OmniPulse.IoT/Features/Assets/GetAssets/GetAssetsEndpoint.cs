using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.IoT.Features.Assets.GetAssets;

public static class GetAssetsEndpoint
{
    public static IEndpointRouteBuilder MapGetAssetsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/iot/assets", async (
                [FromQuery] string? typeFilter,
                [FromQuery] Guid? parentAssetId,
                [FromQuery] Guid? rootAssetId,
                [FromQuery] Guid? responsibleUserId,
                [FromQuery] bool includeDescendants,
                ISender mediator) =>
            {
                var query = new GetAssetsQuery(
                    TypeFilter: typeFilter,
                    ParentAssetId: parentAssetId,
                    RootAssetId: rootAssetId,
                    ResponsibleUserId: responsibleUserId,
                    IncludeDescendants: includeDescendants
                );

                var response = await mediator.Send(query);
                return Results.Ok(response);
            })
            .WithName("GetAssets")
            .WithTags("IoT Assets")
            .WithSummary("Asset listesi — Recursive CTE destekli (tek kök & çok köklü)")
            .WithDescription(
                "Normal mod: typeFilter / parentAssetId ile filtrele. " +
                "Tek kök recursive: rootAssetId + includeDescendants=true. " +
                "Çok köklü recursive: responsibleUserId + includeDescendants=true " +
                "(kullanıcının tüm sorumlu ağaçlarını tek sorguda getirir).")
            .RequireAuthorization(); // JWT Authentication is required! 🔐


        return app;
    }
}
