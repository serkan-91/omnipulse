using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Alarms.GetAlarmRules;

public class GetAlarmRulesQueryHandler(IoTDbContext dbContext)
    : IRequestHandler<GetAlarmRulesQuery, List<AlarmRuleDto>>
{
    public async Task<List<AlarmRuleDto>> Handle(GetAlarmRulesQuery request, CancellationToken cancellationToken)
    {
        // Kiracı filtresi otomatik devrede
        var rules = await dbContext.AlarmRules
            .Include(r => r.Device)
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Name)
            .Select(r => new AlarmRuleDto(
                r.Id,
                r.Name,
                r.DeviceId,
                r.Device != null ? r.Device.Name : null,
                r.MetricKey,
                r.ThresholdValue,
                r.ComparisonOperator,
                r.IsActive
            ))
            .ToListAsync(cancellationToken);

        return rules;
    }
}
