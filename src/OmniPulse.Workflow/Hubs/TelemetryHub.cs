using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OmniPulse.Workflow.Hubs;

/// <summary>
/// IoT cihazlarından gelen telemetri verilerini ön yüze (omnipulse-ui) anlık olarak
/// yayınlayan SignalR Hub'ı! 📊⚡
/// Kiracı izolasyonu (TenantId) sayesinde veriler sadece ilgili kiracının yetkili istemcilerine gider.
/// </summary>
public class TelemetryHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantIdClaim = Context.User?.FindFirst("tid")?.Value 
                            ?? Context.User?.FindFirst("tenant_id")?.Value
                            ?? Context.User?.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            // Kullanıcıyı kendi kiracı grubuna ekle 🛡️
            await Groups.AddToGroupAsync(Context.ConnectionId, $"TENANT_GROUP_{tenantId}");
        }
        else
        {
            // Demo/Misafir bağlantıları için ortak gruba ekle 🔓
            await Groups.AddToGroupAsync(Context.ConnectionId, "demo-tenant");
        }

        await base.OnConnectedAsync();
    }
}
