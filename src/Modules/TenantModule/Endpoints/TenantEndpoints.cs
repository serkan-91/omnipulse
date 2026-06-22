using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using OmniPulse.Modules.TenantModule.Features.Tenants.CheckTenant;
using OmniPulse.Modules.TenantModule.Features.Auth.Login;
using OmniPulse.Modules.TenantModule.Features.Auth.Refresh;
using OmniPulse.Modules.TenantModule.Features.Auth.Logout;
using OmniPulse.Modules.TenantModule.Features.Tenants.InviteUser;

namespace OmniPulse.Modules.TenantModule;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCheckTenantEndpoint();
        app.MapLoginEndpoint();
        app.MapRefreshEndpoint();
        app.MapLogoutEndpoint();
        app.MapInviteUserEndpoint();
        return app;
    }
}
