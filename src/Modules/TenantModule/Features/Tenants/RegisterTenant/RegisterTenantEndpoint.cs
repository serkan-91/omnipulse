using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Modules.TenantModule.Features.Tenants.RegisterTenant;

public static class RegisterTenantEndpoint
{
    public static IEndpointRouteBuilder MapRegisterTenantEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/tenants/register", async (
            [FromBody] RegisterTenantRequest request, 
            ISender mediator) =>
            {
                var command = new RegisterTenantCommand(
                    request.CompanyName,
                    request.TenantIdentifier,
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.Password
                );
                var response = await mediator.Send(command);

                if (!response.IsSuccess)
                {
                    return Results.Json(response, statusCode: StatusCodes.Status400BadRequest);
                }

                return Results.Ok(response);
            })
            .WithName("RegisterTenant")
            .WithTags("Tenant Module");

        return app;
    }
}

public record RegisterTenantRequest(
    string CompanyName,
    string TenantIdentifier,
    string FirstName,
    string LastName,
    string Email,
    string Password
);
