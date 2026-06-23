using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace OmniPulse.Tenant.Features.Auth.Logout;

public static class LogoutEndpoint
{
    public static IEndpointRouteBuilder MapLogoutEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/logout", async (
            ISender mediator, 
            HttpContext httpContext) =>
            {
                var user = httpContext.User;
                
                // Get claims
                var jti = user.FindFirst("jti")?.Value 
                          ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2009/09/identity/claims/jwtid")?.Value;

                var expStr = user.FindFirst("exp")?.Value;

                if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(expStr) || !long.TryParse(expStr, out var expSeconds))
                {
                    return Results.Json(new LogoutResponse(false, "Geçersiz veya eksik token iddiaları (claims)."), statusCode: StatusCodes.Status400BadRequest);
                }

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                             ?? user.FindFirst("sub")?.Value;

                var email = user.FindFirst(ClaimTypes.Email)?.Value 
                            ?? user.FindFirst("email")?.Value;

                var tenantIdentifier = user.FindFirst("tenant_identifier")?.Value;

                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = httpContext.Request.Headers.UserAgent.ToString();

                var command = new LogoutCommand(
                    jti,
                    expSeconds,
                    userId,
                    email,
                    tenantIdentifier,
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
            .RequireAuthorization()
            .WithName("UserLogout")
            .WithTags("Authentication");

        return app;
    }
}
