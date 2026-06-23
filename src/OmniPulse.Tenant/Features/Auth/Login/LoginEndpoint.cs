using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Tenant.Features.Auth.Login;

public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/login", async (
            [FromBody] LoginRequest request, 
            ISender mediator, 
            HttpContext httpContext) =>
            {
                // İstemci IP adresi ve User-Agent başlıklarını denetim günlüğü için yakalıyoruz
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = httpContext.Request.Headers.UserAgent.ToString();

                var command = new LoginCommand(
                    request.Email,
                    request.Password,
                    request.TenantIdentifier,
                    ipAddress,
                    userAgent
                );

                var response = await mediator.Send(command);

                if (!response.IsSuccess)
                {
                    // Güvenlik uyarısı: Hatalı denemelerde generic hata döneriz
                    return Results.Json(response, statusCode: StatusCodes.Status400BadRequest);
                }

                return Results.Ok(response);
            })
            .WithName("UserLogin")
            .WithTags("Authentication");

        return app;
    }
}

public record LoginRequest(string Email, string Password, string? TenantIdentifier = null);
