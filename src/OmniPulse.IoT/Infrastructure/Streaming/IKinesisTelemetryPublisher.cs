using System.Threading;
using System.Threading.Tasks;

namespace OmniPulse.IoT.Infrastructure.Streaming;

/// <summary>
/// IoT cihazlarından gelen telemetri akışını Kinesis'e yönlendiren yayımcı arayüzü. 🚀
/// </summary>
public interface IKinesisTelemetryPublisher
{
    Task PublishAsync(string partitionKey, object telemetryData, CancellationToken cancellationToken = default);
    Task PublishRawAsync(string partitionKey, string rawJsonPayload, CancellationToken cancellationToken = default);
}
