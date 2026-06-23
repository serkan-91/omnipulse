using System;
using MediatR;

namespace OmniPulse.IoT.Features.Alarms.CreateAlarmRule;

public record CreateAlarmRuleCommand(
    string Name,
    Guid? DeviceId,
    string MetricKey,
    double ThresholdValue,
    string ComparisonOperator
) : IRequest<CreateAlarmRuleResponse>;

public record CreateAlarmRuleResponse(
    Guid Id,
    string Name,
    Guid? DeviceId,
    string MetricKey,
    double ThresholdValue,
    string ComparisonOperator,
    string Message
);
