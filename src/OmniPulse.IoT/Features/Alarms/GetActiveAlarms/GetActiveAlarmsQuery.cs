using System;
using System.Collections.Generic;
using MediatR;

namespace OmniPulse.IoT.Features.Alarms.GetActiveAlarms;

public record GetActiveAlarmsQuery(
    Guid? CategoryId = null,
    Guid? AssetId = null
) : IRequest<IEnumerable<ActiveAlarmItem>>;

public record ActiveAlarmItem(
    Guid AlarmEventId,
    Guid DeviceId,
    string DeviceName,
    string DeviceSerialNumber,
    Guid? CategoryId,
    Guid? AssetId,
    string? AssetName,
    Guid AlarmRuleId,
    string MetricKey,
    double TriggeredValue,
    double ThresholdValue,
    string Message,
    DateTime TriggeredAtUtc
);
