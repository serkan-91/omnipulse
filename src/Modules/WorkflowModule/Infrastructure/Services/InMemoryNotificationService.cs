using Microsoft.Extensions.Logging;

namespace OmniPulse.Modules.WorkflowModule.Infrastructure.Services;

/// <summary>
/// Geliştirici ve test ortamları için In-Memory bildirim implementasyonu.
/// Üretimde FCM Push, SignalR WebSocket veya SMS ile değiştirilir.
/// </summary>
public partial class InMemoryNotificationService(
    ILogger<InMemoryNotificationService> logger)
    : INotificationService
{
    public Task NotifyTaskAssignedAsync(TaskAssignedNotification notification, CancellationToken cancellationToken = default)
    {
        LogTaskAssigned(
            notification.AssignedUserId,
            notification.WorkflowName,
            notification.AssetName,
            notification.TaskId);

        // TODO (Faz 3): Push Notification (FCM/APNs)
        // TODO (Faz 3): SignalR WebSocket → Dashboard anlık güncelleme
        // TODO (Faz 3): SMS/E-posta yedek kanal

        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "📣 [Bildirim] Görev Atandı! Kullanıcı: {UserId} | Workflow: '{WorkflowName}' | Varlık: {AssetName} | TaskId: {TaskId}")]
    private partial void LogTaskAssigned(Guid userId, string workflowName, string assetName, Guid taskId);
}
