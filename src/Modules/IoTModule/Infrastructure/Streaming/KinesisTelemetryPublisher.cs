using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OmniPulse.Modules.IoTModule.Infrastructure.Streaming;

/// <summary>
/// AWS Kinesis SDK kullanarak telemetrileri yüksek throughput ile yayınlayan implementasyon. 🐼🌪️
/// </summary>
public class KinesisTelemetryPublisher : IKinesisTelemetryPublisher
{
    private readonly IAmazonKinesis _kinesisClient;
    private readonly ILogger<KinesisTelemetryPublisher> _logger;
    private readonly string _streamName;

    public KinesisTelemetryPublisher(
        IAmazonKinesis kinesisClient,
        IConfiguration configuration,
        ILogger<KinesisTelemetryPublisher> logger)
    {
        _kinesisClient = kinesisClient;
        _logger = logger;
        _streamName = configuration.GetValue<string>("AWS:Kinesis:StreamName") ?? "omnipulse-telemetry-stream";
    }

    public async Task PublishAsync(string partitionKey, object telemetryData, CancellationToken cancellationToken = default)
    {
        if (_kinesisClient == null)
        {
            _logger.LogWarning("⚠️ Kinesis istemcisi oluşturulmadığı için telemetri yayını atlandı.");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(telemetryData);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var memoryStream = new MemoryStream(bytes);
            var putRecordRequest = new PutRecordRequest
            {
                StreamName = _streamName,
                PartitionKey = partitionKey,
                Data = memoryStream
            };

            var result = await _kinesisClient.PutRecordAsync(putRecordRequest, cancellationToken);
            _logger.LogInformation("🚀 Telemetri Kinesis'e aktarıldı. Shard: {ShardId}, Seq: {SequenceNumber}", 
                result.ShardId, result.SequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Kinesis'e telemetri yayınlanırken kritik hata! PartitionKey: {Key}", partitionKey);
            throw;
        }
    }
}
