using System;
using System.Collections.Generic;
using MediatR;

namespace OmniPulse.IoT.Features.Alarms.GetAlarmRules;

public record GetAlarmRulesQuery : IRequest<List<AlarmRuleDto>>;

public record AlarmRuleDto(
    Guid Id,
    string Name,
    Guid? DeviceId,
    string? DeviceName,
    string MetricKey,
    double ThresholdValue,
    string ComparisonOperator,
    bool IsActive
);
