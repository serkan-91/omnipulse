using System;

namespace OmniPulse.Modules.WorkflowModule.Domain.Entities;

/// <summary>
/// DynamoDB üzerinde tutulacak olan Görev (Execution Instance) modeli! ⚡
/// TenantId (PK) ve TaskId (SK) hiyerarşisi ile yüksek ölçeklenebilirlik sağlar.
/// </summary>
public class WorkflowTask
{
    // Partition Key (PK) -> TenantId (Örn: "TENANT#<guid>")
    public string PK { get; set; } = null!;

    // Sort Key (SK) -> TASK#<guid>
    public string SK { get; set; } = null!;

    public Guid TenantId { get; set; }
    public Guid TaskId { get; set; }

    public Guid WorkflowDefinitionId { get; set; }
    public string WorkflowName { get; set; } = null!;

    public Guid AssetId { get; set; }
    public string AssetName { get; set; } = null!;
    public string AssetType { get; set; } = null!;

    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Cancelled
    public string Description { get; set; } = null!;

    public Guid? AssignedUserId { get; set; }

    // Mükerrer eylemleri engellemek için kaynak olay kimliği (Idempotency)
    public string SourceEventId { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Yarış durumlarını (Race Conditions) engellemek için Versiyon alanı (Optimistic Concurrency)
    public int Version { get; set; } = 1;

    // Çalışma anı telemetrisi ve meta verileri
    public string? ExecutionContextJson { get; set; }
}
