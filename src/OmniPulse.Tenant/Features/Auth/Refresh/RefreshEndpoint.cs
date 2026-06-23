using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Tenant.Features.Auth.Refresh;

public static class RefreshEndpoint
{
    public static IEndpointRouteBuilder MapRefreshEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/refresh", async (
            [FromBody] RefreshRequest request, 
            ISender mediator, 
            HttpContext httpContext) =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = httpContext.Request.Headers.UserAgent.ToString();

                var command = new RefreshCommand(
                    request.Token,
                    request.RefreshToken,
                    ipAddress,
                    userAgent
                );

                var response = await mediator.Send(command);

                if (!response.IsSuccess)
                {
                    return Results.Json(response, statusCode: StatusCodes.Status400BadRequest);
                }

                return Results.Ok(response);
            })
            .WithName("RefreshToken")
            .WithTags("Authentication");

        return app;
    }
}

public record RefreshRequest(string Token, string RefreshToken);
