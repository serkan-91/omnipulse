using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.IoT.Features.Assets.CreateAsset;

public static class CreateAssetEndpoint
{
    public static IEndpointRouteBuilder MapCreateAssetEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/assets", async (CreateAssetDto dto, ISender mediator) =>
            {
                var command = new CreateAssetCommand(
                    Type: dto.Type,
                    Name: dto.Name,
                    ParentAssetId: dto.ParentAssetId,
                    ResponsibleUserId: dto.ResponsibleUserId,
                    MetadataJson: dto.MetadataJson
                );

                var response = await mediator.Send(command);
                return Results.Created($"api/iot/assets/{response.Id}", response);
            })
            .WithName("CreateAsset")
            .WithTags("IoT Assets")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}

public record CreateAssetDto(
    string Type,
    string Name,
    Guid? ParentAssetId,
    Guid? ResponsibleUserId,
    string? MetadataJson
);
