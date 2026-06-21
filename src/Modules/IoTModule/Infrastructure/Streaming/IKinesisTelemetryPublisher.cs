using System.Threading;
using System.Threading.Tasks;

namespace OmniPulse.Modules.IoTModule.Infrastructure.Streaming;

/// <summary>
/// IoT cihazlarından gelen telemetri akışını Kinesis'e yönlendiren yayımcı arayüzü. 🚀
/// </summary>
public interface IKinesisTelemetryPublisher
{
    Task PublishAsync(string partitionKey, object telemetryData, CancellationToken cancellationToken = default);
}
