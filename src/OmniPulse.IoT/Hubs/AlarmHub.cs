using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OmniPulse.IoT.Hubs;

/// <summary>
/// IoT alarmlarını teknik ekiplere (Mehmet Usta) anlık olarak yayınlayan SignalR Hub'ı! 🚨⚡
/// Kiracı izolasyonu sayesinde (TenantId grubu), hiçbir sızıntı yaşanmadan sadece ilgili kiracının
/// yetkili personeline canlı veri akışı sağlanır.
/// </summary>
[Authorize]
public class AlarmHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantIdClaim = Context.User?.FindFirst("tid")?.Value 
                            ?? Context.User?.FindFirst("tenant_id")?.Value
                            ?? Context.User?.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            // Kullanıcıyı kiracısına özel SignalR grubuna ekliyoruz 🛡️
            await Groups.AddToGroupAsync(Context.ConnectionId, $"TENANT_GROUP_{tenantId}");
        }

        await base.OnConnectedAsync();
    }
}
