using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.IoT.Domain.Entities;
using OmniPulse.IoT.Infrastructure.Persistence;

namespace OmniPulse.IoT.Features.Alarms.CreateAlarmRule;

public class CreateAlarmRuleCommandHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<CreateAlarmRuleCommand, CreateAlarmRuleResponse>
{
    public async Task<CreateAlarmRuleResponse> Handle(CreateAlarmRuleCommand request, CancellationToken cancellationToken)
    {
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;

        // MetricKey doğrulaması
        var metricNormalized = request.MetricKey.Trim();
        if (!metricNormalized.Equals("Temperature", StringComparison.OrdinalIgnoreCase) &&
            !metricNormalized.Equals("Pressure", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Desteklenen metrik anahtarları sadece 'Temperature' ve 'Pressure' dır!");
        }

        // Karşılaştırma operatörü doğrulaması
        var op = request.ComparisonOperator.Trim();
        if (op != ">" && op != "<" && op != ">=" && op != "<=" && op != "==")
        {
            throw new ArgumentException("Desteklenen karşılaştırma operatörleri: '>', '<', '>=', '<=', '=='");
        }

        // Cihaz doğrulaması (belirtilmişse)
        if (request.DeviceId.HasValue)
        {
            var deviceExists = await dbContext.Devices
                .AnyAsync(d => d.Id == request.DeviceId.Value, cancellationToken);

            if (!deviceExists)
            {
                throw new KeyNotFoundException($"Belirtilen cihaz [{request.DeviceId.Value}] bulunamadı veya bu kiracıya ait değil!");
            }
        }

        var rule = AlarmRule.Create(
            tenantId,
            request.Name,
            request.DeviceId,
            metricNormalized,
            request.ThresholdValue,
            op
        );

        dbContext.AlarmRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateAlarmRuleResponse(
            Id: rule.Id,
            Name: rule.Name,
            DeviceId: rule.DeviceId,
            MetricKey: rule.MetricKey,
            ThresholdValue: rule.ThresholdValue,
            ComparisonOperator: rule.ComparisonOperator,
            Message: $"Alarm kuralı [{rule.Name}] başarıyla oluşturuldu! 🚨"
        );
    }
}
