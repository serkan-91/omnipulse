using System.Threading.Channels;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;

/// <summary>
/// Telemetri verilerini kuyruğa alıp arka planda asenkron işlemek için
/// hafif ve yüksek performanslı kanal sarmalayıcı! ⚡🔋
/// </summary>
public class TelemetryQueue
{
    private readonly Channel<TelemetryIngestedEvent> _channel = Channel.CreateUnbounded<TelemetryIngestedEvent>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelWriter<TelemetryIngestedEvent> Writer => _channel.Writer;
    public ChannelReader<TelemetryIngestedEvent> Reader => _channel.Reader;
}
