using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.IoT.Infrastructure.Persistence;

namespace OmniPulse.IoT.Features.Alarms.GetActiveAlarms;

public class GetActiveAlarmsQueryHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<GetActiveAlarmsQuery, IEnumerable<ActiveAlarmItem>>
{
    public async Task<IEnumerable<ActiveAlarmItem>> Handle(GetActiveAlarmsQuery request, CancellationToken cancellationToken)
    {
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;

        // 1. Aktif (çözülmemiş) alarmları sorgula
        var query = dbContext.AlarmEvents
            .Include(e => e.Device)
            .ThenInclude(d => d.Asset)
            .Include(e => e.AlarmRule)
            .Where(e => e.TenantId == tenantId && !e.IsResolved);

        // 2. Kategoriye göre filtrele (Sektör bazlı)
        if (request.CategoryId.HasValue)
        {
            query = query.Where(e => e.Device.CategoryId == request.CategoryId.Value);
        }

        // 3. Varlığa göre filtrele (Hiyerarşik desteğiyle: kendisi veya alt kırılımları)
        if (request.AssetId.HasValue)
        {
            var assetId = request.AssetId.Value;
            query = query.Where(e => e.Device.AssetId == assetId || 
                                     (e.Device.Asset != null && e.Device.Asset.ParentAssetId == assetId));
        }

        // 4. Sonuçları listele ve sırala (en son tetiklenen en üstte)
        var alarmList = await query
            .OrderByDescending(e => e.TriggeredAtUtc)
            .ToListAsync(cancellationToken);

        return alarmList.Select(e => new ActiveAlarmItem(
            AlarmEventId: e.Id,
            DeviceId: e.DeviceId,
            DeviceName: e.Device.Name,
            DeviceSerialNumber: e.Device.SerialNumber,
            CategoryId: e.Device.CategoryId,
            AssetId: e.Device.AssetId,
            AssetName: e.Device.Asset?.Name,
            AlarmRuleId: e.AlarmRuleId,
            MetricKey: e.AlarmRule.MetricKey,
            TriggeredValue: e.TriggeredValue,
            ThresholdValue: e.ThresholdValue,
            Message: e.Message,
            TriggeredAtUtc: e.TriggeredAtUtc
        ));
    }
}
