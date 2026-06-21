using OmniPulse.Modules.WorkflowModule.Domain.Entities;

namespace OmniPulse.Modules.WorkflowModule.Infrastructure.Services;

public interface IWorkflowTaskStore
{
    /// <summary>
    /// Görevi DynamoDB'ye kaydeder veya günceller.
    /// Concurrency: Version alanı ve ConditionalUpdate (beklenen versiyon kontrolü) kullanır.
    /// </summary>
    Task SaveTaskAsync(WorkflowTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// PK (TenantId) ve SK (TaskId) ile tek bir görevi getirir.
    /// </summary>
    Task<WorkflowTask?> GetTaskAsync(Guid tenantId, Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen ekin (sourceEventId) bu kiracı için işlenip işlenmediğini sorgular (Idempotency).
    /// </summary>
    Task<bool> HasEventBeenProcessedAsync(Guid tenantId, string sourceEventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir kiracıya ait görevleri listeler. Opsiyonel olarak durum (status) filtresi verilebilir.
    /// Durum filtresi verildiğinde DynamoDB TenantStatusIndex GSI'ı tetiklenir ve maliyet minimize edilir.
    /// </summary>
    Task<IEnumerable<WorkflowTask>> GetTasksByTenantAsync(Guid tenantId, string? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aynı varlık ve workflow için zaten aktif (Pending veya InProgress) bir görev var mı?
    /// Aktif Kart Spam Koruması için kullanılır.
    /// </summary>
    Task<bool> HasActiveTaskAsync(Guid tenantId, Guid assetId, Guid workflowDefinitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir görevin durumunu günceller. Optimistic concurrency için Version kontrolü yapar.
    /// </summary>
    Task UpdateTaskStatusAsync(WorkflowTask task, string newStatus, CancellationToken cancellationToken = default);
}
