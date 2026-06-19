using OmniPulse.Modules.TenantModule.Features.Tenants.CheckTenant;

namespace OmniPulse.Identity.API.Endpoints;

public static class TenantEndpoints
{
    // Minimal API endpoint'lerimizi dikey dilimlerimize yönlendiren şanlı kapı! 🚀
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCheckTenantEndpoint();
        return app;
    }
}

