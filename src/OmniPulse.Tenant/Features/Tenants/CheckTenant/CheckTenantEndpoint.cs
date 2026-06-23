using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Tenant.Features.Tenants.CheckTenant;

// Dikey Dilimimizin Dış Dünyaya Açılan Minimal API Kapısı 🍕
public static class CheckTenantEndpoint
{
    public static IEndpointRouteBuilder MapCheckTenantEndpoint(this IEndpointRouteBuilder app)
    {
        // MomoYuki tünelinden sorgulayan o tatlı GET endpoint'i! 🏎️💨
        app.MapGet("api/tenants/current-status", async (ISender mediator) =>
            {
                var response = await mediator.Send(new CheckTenantCommand());
                return Results.Ok(response);
            })
            .WithName("CheckCurrentTenantStatus")
            .WithTags("Tenants Management");

        return app;
    }
}