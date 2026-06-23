using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Tenant.Features.Tenants.InviteUser;

public static class InviteUserEndpoint
{
    public static IEndpointRouteBuilder MapInviteUserEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/tenants/invite", async (
            [FromBody] InviteUserRequest request, 
            ISender mediator) =>
            {
                var command = new InviteUserCommand(request.Email, request.Role);
                var response = await mediator.Send(command);

                if (!response.IsSuccess)
                {
                    return Results.Json(response, statusCode: StatusCodes.Status400BadRequest);
                }

                return Results.Ok(response);
            })
            .RequireAuthorization()
            .WithName("InviteUserToTenant")
            .WithTags("Tenants");

        return app;
    }
}

public record InviteUserRequest(string Email, string Role);
