using System;
using OmniPulse.BuildingBlocks.Common;

namespace OmniPulse.IoT.Domain.Entities;

/// <summary>
/// Transactional Outbox Pattern için Kinesis'e gönderilecek olayları veri tabanında tutan entity. 📦
/// </summary>
public class OutboxEvent : BaseEntity
{
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public string PartitionKey { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    private OutboxEvent() { }

    public static OutboxEvent Create(string eventType, string payload, string partitionKey)
    {
        return new OutboxEvent
        {
            EventType = eventType,
            Payload = payload,
            PartitionKey = partitionKey,
            CreatedAtUtc = DateTime.UtcNow,
            AttemptCount = 0
        };
    }

    public void IncrementAttempt(string error)
    {
        AttemptCount++;
        LastError = error;
    }
}
