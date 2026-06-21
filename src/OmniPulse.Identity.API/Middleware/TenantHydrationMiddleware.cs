using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.TenantModule.Features.Common.Interfaces;
using OmniPulse.Modules.TenantModule.Infrastructure.Persistence;

namespace OmniPulse.Identity.API.Middleware;

/// <summary>
/// Gelen isteklerdeki JWT (tid claim) veya istek başlığı (X-Tenant-Id / X-Tenant-Identifier) üzerinden
/// kiracıyı (Tenant) koklayan ve doğruluğunu veri tabanından teyit eden koruyucu kalkan! 🛡️🚦
/// </summary>
public class TenantHydrationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IUserTenantContext userTenantContext,
        ITenantService tenantService,
        IdentityDbContext dbContext)
    {
        // 1. Zaten el ile set edilmiş bir kiracı varsa dokunma (örn. testlerde)
        if (tenantService.GetCurrentTenant() != null)
        {
            await next(context);
            return;
        }

        var tenantId = userTenantContext.TenantId;
        var tenantIdentifier = userTenantContext.TenantIdentifier;

        // 2. Kiracı çözümlenmeye çalışılıyor mu?
        if (tenantId.HasValue)
        {
            // Veri tabanında bu ID'ye sahip kiracıyı arıyoruz
            var tenant = await dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId.Value);

            if (tenant == null)
            {
                await WriteForbiddenResponse(context, "Talep edilen kiracı (Tenant) sistemde bulunamadı.");
                return;
            }

            // Kiracı aktiflik, silinme ve abonelik kontrollerini yapıyoruz
            if (!tenant.IsActive || tenant.IsDeleted)
            {
                await WriteForbiddenResponse(context, "Bu şirketin/kiracının erişimi askıya alınmıştır.");
                return;
            }

            if (tenant.SubscriptionEndDate < DateTime.UtcNow)
            {
                await WriteForbiddenResponse(context, "Bu şirketin/kiracının abonelik süresi sona ermiştir.");
                return;
            }

            // Kiracıyı context içerisine hydrate (enjekte) ediyoruz 💧
            tenantService.SetTenant(tenant);
        }
        else if (!string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            // Eğer Guid ID yok ama string takma ad (Örn: "pandaberry") varsa oradan arıyoruz
            var identifier = tenantIdentifier.ToLowerInvariant().Trim();
            var tenant = await dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Identifier == identifier);

            if (tenant != null)
            {
                if (!tenant.IsActive || tenant.IsDeleted)
                {
                    await WriteForbiddenResponse(context, "Bu şirketin/kiracının erişimi askıya alınmıştır.");
                    return;
                }

                if (tenant.SubscriptionEndDate < DateTime.UtcNow)
                {
                    await WriteForbiddenResponse(context, "Bu şirketin/kiracının abonelik süresi sona ermiştir.");
                    return;
                }

                tenantService.SetTenant(tenant);
            }
        }

        await next(context);
    }

    private static async Task WriteForbiddenResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var responseObj = new { isSuccess = false, message };
        var json = JsonSerializer.Serialize(responseObj);
        await context.Response.WriteAsync(json);
    }
}
