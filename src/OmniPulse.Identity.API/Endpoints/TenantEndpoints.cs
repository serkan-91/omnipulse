using OmniPulse.Modules.TenantModule.Features.Tenants.CheckTenant;
using OmniPulse.Modules.TenantModule.Features.Auth.Login;
using OmniPulse.Modules.TenantModule.Features.Auth.Refresh;
using OmniPulse.Modules.TenantModule.Features.Tenants.InviteUser;

namespace OmniPulse.Identity.API.Endpoints;

public static class TenantEndpoints
{
    // Minimal API endpoint'lerimizi dikey dilimlerimize yönlendiren şanlı kapı! 🚀
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCheckTenantEndpoint();
        app.MapLoginEndpoint();
        app.MapRefreshEndpoint();
        app.MapInviteUserEndpoint();
        return app;
    }
}

