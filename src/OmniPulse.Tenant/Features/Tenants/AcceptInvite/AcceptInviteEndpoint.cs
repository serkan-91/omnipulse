using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Tenant.Features.Tenants.AcceptInvite;

public static class AcceptInviteEndpoint
{
    public static IEndpointRouteBuilder MapAcceptInviteEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/tenants/accept-invite", async (
            [FromBody] AcceptInviteRequest request, 
            ISender mediator) =>
            {
                var command = new AcceptInviteCommand(
                    request.Email,
                    request.FirstName,
                    request.LastName,
                    request.Password
                );
                var response = await mediator.Send(command);

                if (!response.IsSuccess)
                {
                    return Results.Json(response, statusCode: StatusCodes.Status400BadRequest);
                }

                return Results.Ok(response);
            })
            .WithName("AcceptInvite")
            .WithTags("Tenant Module");

        return app;
    }
}

public record AcceptInviteRequest(
    string Email,
    string FirstName,
    string LastName,
    string Password
);
