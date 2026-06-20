using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.GetCategoryTree;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.CreateCategory;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.UpdateCategory;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.DeleteCategory;

namespace OmniPulse.Modules.IoTModule.Features.DeviceCategories;

/// <summary>
/// Kategori ağacı okumalarını (GetCategoryTreeQuery) kiracı bazlı olarak önbelleğe alan (caching)
/// ve kategori üzerinde herhangi bir yazma/güncelleme/silme işlemi yapıldığında önbelleği 
/// otomatik olarak temizleyen (cache invalidation) modern MediatR boru hattı davranışı! 🌳🏷️⚡
/// </summary>
public class DeviceCategoryCacheBehavior<TRequest, TResponse>(
    IMemoryCache cache,
    IUserTenantContext userTenantContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Kiracı bağlamını alalım
        var tenantId = userTenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            return await next();
        }

        // Önbellek anahtarı her kiracı için benzersizdir (veri sızıntısını önler) 🛡️
        var cacheKey = $"DeviceCategoryTree_{tenantId.Value}";

        // 1. İstek Okuma (Query) ise: Önbelleği kullan
        if (request is GetCategoryTreeQuery)
        {
            if (cache.TryGetValue(cacheKey, out TResponse? cachedResponse) && cachedResponse != null)
            {
                return cachedResponse;
            }

            var response = await next();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            cache.Set(cacheKey, response, cacheOptions);

            return response;
        }

        // 2. İstek Yazma (Command) ise: İşlem sonrası önbelleği temizle (Cache Invalidation)
        if (request is CreateCategoryCommand || 
            request is UpdateCategoryCommand || 
            request is DeleteCategoryCommand)
        {
            var response = await next();
            
            // Veritabanı işlemi başarılı olduysa ilgili kiracının önbelleğini uçuruyoruz!
            cache.Remove(cacheKey);

            return response;
        }

        return await next();
    }
}
