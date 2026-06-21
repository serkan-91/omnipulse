using System;

namespace OmniPulse.Modules.WorkflowModule.Infrastructure.Services;

/// <summary>Görev atandığında bildirim kanallarına iletilen veri nesnesi.</summary>
public class TaskAssignedNotification
{
    public Guid AssignedUserId { get; init; }
    public Guid TaskId { get; init; }
    /// <summary>Varlık adı (örn: "Bant A1", "AUPanda01").</summary>
    public string AssetName { get; init; } = null!;
    /// <summary>Tetiklenen iş akışı adı (örn: "Suyu kontrol et!").</summary>
    public string WorkflowName { get; init; } = null!;
    public string Description { get; init; } = null!;
    public Guid TenantId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
