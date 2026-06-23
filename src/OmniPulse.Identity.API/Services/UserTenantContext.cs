using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Identity.API.Services;

/// <summary>
/// HttpContext üzerinden o anki isteğin JWT/Claims yapısını okuyarak hem USER (Kullanıcı)
/// hem de TENANT (Kiracı) bağlamını bir arada sunan kurumsal güvenlik servisi! 🛡️⚡
/// </summary>
public class UserTenantContext(IHttpContextAccessor httpContextAccessor) : IUserTenantContext
{
    public string? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                   ?? user?.FindFirst("sub")?.Value;
        }
    }

    public string? Email
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Email)?.Value 
                   ?? user?.FindFirst("email")?.Value;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            
            // Microsoft standartlarındaki 'tid' (Tenant ID) veya 'tenant_id' claim'ini okuruz
            var tenantIdStr = user?.FindFirst("tid")?.Value 
                              ?? user?.FindFirst("tenant_id")?.Value
                              ?? user?.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

            if (Guid.TryParse(tenantIdStr, out var tenantId))
            {
                return tenantId;
            }

            // Eğer JWT içinde yoksa (yani kullanıcı istek başlığında X-Tenant-Id göndermişse) oradan okuyalım
            var tenantHeader = httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (Guid.TryParse(tenantHeader, out var headerTenantId))
            {
                return headerTenantId;
            }

            return null;
        }
    }

    public string? TenantIdentifier
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.FindFirst("tenant_identifier")?.Value 
                   ?? httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Identifier"].FirstOrDefault();
        }
    }

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user == null) return [];

            return user.FindAll(ClaimTypes.Role).Select(c => c.Value)
                .Concat(user.FindAll("roles").Select(c => c.Value));
        }
    }
}
