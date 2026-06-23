using System.Threading;
using System.Threading.Tasks;

namespace OmniPulse.Workflow.Infrastructure.Services;

/// <summary>
/// Görev atama bildirimlerini ilgili kullanıcıya ileten servis arayüzü.
/// Geliştirici: InMemory (loglama). Üretim: Push/WebSocket/SMS.
/// </summary>
public interface INotificationService
{
    Task NotifyTaskAssignedAsync(TaskAssignedNotification notification, CancellationToken cancellationToken = default);
}
