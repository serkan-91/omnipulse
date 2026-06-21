using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;

namespace OmniPulse.Modules.IoTModule.Features.Assets.AssignResponsibleUser;

public static class AssignResponsibleUserEndpoint
{
    public static IEndpointRouteBuilder MapAssignResponsibleUserEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/iot/assets/{id:guid}/assign-responsible",
            async (Guid id, AssignResponsibleUserDto dto, ISender mediator) =>
            {
                var response = await mediator.Send(new AssignResponsibleUserCommand(id, dto.UserId, dto.Role, dto.IsUnassign));
                return Results.Ok(response);
            })
            .WithName("AssignResponsibleUserToAsset")
            .WithTags("IoT Assets")
            .RequireAuthorization(); // JWT Authentication is required! 🔐

        return app;
    }
}

public record AssignResponsibleUserDto(Guid UserId, string Role, bool IsUnassign = false);
