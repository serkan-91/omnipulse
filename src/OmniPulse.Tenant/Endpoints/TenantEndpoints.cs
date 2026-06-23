using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using OmniPulse.Tenant.Features.Tenants.CheckTenant;
using OmniPulse.Tenant.Features.Auth.Login;
using OmniPulse.Tenant.Features.Auth.Refresh;
using OmniPulse.Tenant.Features.Auth.Logout;
using OmniPulse.Tenant.Features.Tenants.InviteUser;
using OmniPulse.Tenant.Features.Tenants.RegisterTenant;
using OmniPulse.Tenant.Features.Tenants.AcceptInvite;

namespace OmniPulse.Tenant;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCheckTenantEndpoint();
        app.MapLoginEndpoint();
        app.MapRefreshEndpoint();
        app.MapLogoutEndpoint();
        app.MapInviteUserEndpoint();
        app.MapRegisterTenantEndpoint();
        app.MapAcceptInviteEndpoint();
        return app;
    }
}
