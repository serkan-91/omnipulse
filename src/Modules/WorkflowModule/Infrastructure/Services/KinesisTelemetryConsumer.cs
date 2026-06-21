using System.Collections.Concurrent;
using System.Text.Json;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using OmniPulse.Modules.WorkflowModule.Features.Workflows.ProcessTelemetryEvent;
using OmniPulse.Modules.WorkflowModule.Hubs;

namespace OmniPulse.Modules.WorkflowModule.Infrastructure.Services;

public class TelemetryEventPayload
{
    public Guid TenantId { get; init; }
    public Guid DeviceId { get; init; }
    public string? TelemetryKey { get; init; }
    public double? TelemetryValue { get; init; }
    public string? EventId { get; init; }
    public DateTime? Timestamp { get; init; }

    // IoT Module specific properties
    public Guid? TelemetryId { get; init; }
    public string? DeviceSerialNumber { get; init; }
    public double? Temperature { get; init; }
    public double? Pressure { get; init; }

    // Security & Status Tracking fields
    public string? EventType { get; init; }
    public string? Action { get; init; }
    public string? Message { get; init; }
    public bool? IsOnline { get; init; }
    public string? TraceId { get; init; }
}

/// <summary>
/// AWS Kinesis Data Streams tüketici servisi! 🌊🔋
/// Kinesis'ten akan telemetri verilerini okur, iş akışı motoruna gönderir.
/// Hatalı/bozuk mesajları SQS Dead Letter Queue (DLQ) kuyruğuna güvenle park eder.
/// </summary>
public partial class KinesisTelemetryConsumer : BackgroundService
{
    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("OmniPulse.WorkflowModule.KinesisTelemetryConsumer");

    private readonly IAmazonKinesis? _kinesis;
    private readonly IAmazonSQS? _sqs;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KinesisTelemetryConsumer> _logger;
    private readonly IHubContext<TelemetryHub>? _hubContext;
    private readonly string _streamName;
    private readonly string _dlqQueueUrl;
    private readonly bool _isEnabled;
    private readonly ConcurrentDictionary<string, Task> _activeShardTasks = new();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public KinesisTelemetryConsumer(
        IAmazonKinesis? kinesis,
        IAmazonSQS? sqs,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<KinesisTelemetryConsumer> logger,
        IHubContext<TelemetryHub>? hubContext = null)
    {
        _kinesis = kinesis;
        _sqs = sqs;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hubContext = hubContext;
        _streamName = configuration.GetValue<string>("AWS:Kinesis:StreamName") ?? "omnipulse-telemetry-stream";
        _dlqQueueUrl = configuration.GetValue<string>("AWS:SQS:QueueUrl") ?? "https://sqs.us-east-1.amazonaws.com/123456789012/OmniPulse_Telemetry_DLQ";

        try
        {
            _isEnabled = _kinesis?.Config != null && 
                         (!string.IsNullOrEmpty(_kinesis.Config.RegionEndpoint?.SystemName) || 
                          !string.IsNullOrEmpty(_kinesis.Config.ServiceURL));
        }
        catch
        {
            _isEnabled = false;
        }

        if (!_isEnabled)
        {
            LogAwsConnectionFailed();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isEnabled)
        {
            // Dev modunda simülasyon veya bekleme
            while (!stoppingToken.IsCancellationRequested)
            {
                LogKinesisPassiveMode();
                await Task.Delay(15000, stoppingToken);
            }
            return;
        }

        LogConsumerStarted(_streamName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Biten, iptal olan veya hata veren task'ları sözlükten temizle (ekstra güvenlik önlemi)
                foreach (var kvp in _activeShardTasks)
                {
                    if (kvp.Value.IsCompleted)
                    {
                        _activeShardTasks.TryRemove(kvp.Key, out _);
                    }
                }

                // 1. Shard'ları listele
                var describeRequest = new DescribeStreamRequest { StreamName = _streamName };
                var describeResponse = await _kinesis!.DescribeStreamAsync(describeRequest, stoppingToken);
                var shards = describeResponse.StreamDescription.Shards;

                foreach (var shard in shards)
                {
                    var shardId = shard.ShardId;

                    // Eğer bu shard şu an aktif olarak işlenmiyorsa, yeni bir task başlat
                    if (_activeShardTasks.ContainsKey(shardId)) continue;
                    var task = Task.Run(() => ProcessShardAsync(shardId, stoppingToken), stoppingToken);
                    _activeShardTasks.TryAdd(shardId, task);
                }
            }
            catch (Exception ex)
            {
                LogReadLoopError(ex);
            }

            try
            {
                // Shard listesini güncellemek için bekleme (örn. 15 saniye)
                await Task.Delay(15000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // Tüketici durdurulduğunda tüm aktif shard işlemlerinin tamamlanmasını bekle
        try
        {
            await Task.WhenAll(_activeShardTasks.Values);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tüketici durdurulurken bazı shard işlemleri hata fırlattı.");
        }
    }

    private async Task ProcessShardAsync(string shardId, CancellationToken cancellationToken)
    {
        try
        {
            var iteratorRequest = new GetShardIteratorRequest
            {
                StreamName = _streamName,
                ShardId = shardId,
                ShardIteratorType = ShardIteratorType.LATEST
            };

            var iteratorResponse = await _kinesis!.GetShardIteratorAsync(iteratorRequest, cancellationToken);
            var iterator = iteratorResponse.ShardIterator;

            // Shard'dan kayıtları oku
            while (iterator != null && !cancellationToken.IsCancellationRequested)
            {
                var getRecordsRequest = new GetRecordsRequest
                {
                    ShardIterator = iterator,
                    Limit = 100
                };

                var getRecordsResponse = await _kinesis!.GetRecordsAsync(getRecordsRequest, cancellationToken);
                foreach (var record in getRecordsResponse.Records)
                {
                    await ProcessRecordWithRetryAsync(record, cancellationToken);
                }

                iterator = getRecordsResponse.NextShardIterator;
                await Task.Delay(1000, cancellationToken); // Kinesis rate limit'i aşmamak için bekleme
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Uygulama kapanırken iptal olması normaldir
        }
        catch (Exception ex)
        {
            LogShardProcessError(ex, shardId);
        }
        finally
        {
            _activeShardTasks.TryRemove(shardId, out _);
        }
    }

    private async Task ProcessRecordWithRetryAsync(Record record, CancellationToken cancellationToken)
    {
        string rawData = string.Empty;
        try
        {
            using var reader = new StreamReader(record.Data);
            rawData = await reader.ReadToEndAsync(cancellationToken);

            var payload = JsonSerializer.Deserialize<TelemetryEventPayload>(rawData, SerializerOptions);

            if (payload == null)
            {
                throw new InvalidDataException("Kinesis mesaj verisi boş veya geçersiz!");
            }

            // OpenTelemetry Dağıtık İzlenebilirlik (Distributed Tracing) ve APM Bağlama 🔗 OTel
            var traceParent = payload.TraceId;
            System.Diagnostics.Activity? activity = null;

            if (!string.IsNullOrEmpty(traceParent) && System.Diagnostics.ActivityContext.TryParse(traceParent, null, out var parentContext))
            {
                activity = ActivitySource.StartActivity("ProcessKinesisRecord", System.Diagnostics.ActivityKind.Consumer, parentContext);
            }
            else
            {
                activity = ActivitySource.StartActivity("ProcessKinesisRecord", System.Diagnostics.ActivityKind.Consumer);
            }

            using (activity)
            {
                var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? payload.TraceId ?? Guid.NewGuid().ToString();
                using (_logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = traceId }))
                {
                    // 1. Güvenlik Olayı (Security Audit) Yönlendirme 🚨
                    if (string.Equals(payload.EventType, "SecurityAudit", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("🚨 [SECURITY AUDIT] Action: {Action} | Device: {Serial} | Message: {Msg}",
                            payload.Action, payload.DeviceSerialNumber, payload.Message);

                        if (_hubContext != null)
                        {
                            var tenantGroup = payload.TenantId != Guid.Empty ? payload.TenantId.ToString() : "demo-tenant";
                            await _hubContext.Clients.Groups(tenantGroup, "demo-tenant").SendAsync(
                                "ReceiveSecurityAlert",
                                new
                                {
                                    payload.EventType,
                                    payload.Action,
                                    payload.DeviceSerialNumber,
                                    payload.Message,
                                    Timestamp = DateTime.UtcNow
                                },
                                cancellationToken
                            );
                        }
                        return; // Workflow tetiklemeyi atla
                    }

                    // 2. Cihaz Durum Değişikliği (Device Connection) Yönlendirme 🔌
                    if (string.Equals(payload.EventType, "DeviceConnection", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("[DEVICE CONNECTION] Device: {Serial} is now {Status}",
                            payload.DeviceSerialNumber, payload.IsOnline == true ? "ONLINE" : "OFFLINE");

                        if (_hubContext != null)
                        {
                            var tenantGroup = payload.TenantId != Guid.Empty ? payload.TenantId.ToString() : "demo-tenant";
                            await _hubContext.Clients.Groups(tenantGroup, "demo-tenant").SendAsync(
                                "ReceiveDeviceStatus",
                                new
                                {
                                    payload.EventType,
                                    payload.DeviceSerialNumber,
                                    payload.IsOnline,
                                    payload.Message,
                                    Timestamp = DateTime.UtcNow
                                },
                                cancellationToken
                            );
                        }
                        return; // Workflow tetiklemeyi atla
                    }

                    var eventsToProcess = new List<ProcessTelemetryEventCommand>();

                    if (!string.IsNullOrEmpty(payload.TelemetryKey))
                    {
                        // Standart/Jenerik Telemetri formatı
                        // CihazID ve Zaman damgasından üretilen deterministik EventId sayesinde mükerrer telemetriler yakalanır! 🛡️
                        var eventId = payload.EventId;
                        if (string.IsNullOrEmpty(eventId))
                        {
                            var ts = payload.Timestamp ?? DateTime.UtcNow;
                            eventId = $"GEN-{payload.DeviceId}-{ts:yyyyMMddHHmmss}";
                        }

                        eventsToProcess.Add(new ProcessTelemetryEventCommand(
                            TenantId: payload.TenantId,
                            DeviceId: payload.DeviceId,
                            TelemetryKey: payload.TelemetryKey,
                            TelemetryValue: payload.TelemetryValue ?? 0.0,
                            SourceEventId: eventId
                        ));
                    }
                    else if (payload.Temperature.HasValue || payload.Pressure.HasValue)
                    {
                        // IoT Modülü spesifik formatı (Sıcaklık ve Basınç alanları içeren paket)
                        var eventIdBase = payload.TelemetryId?.ToString();
                        if (string.IsNullOrEmpty(eventIdBase))
                        {
                            var ts = payload.Timestamp ?? DateTime.UtcNow;
                            eventIdBase = $"{payload.DeviceId}-{ts:yyyyMMddHHmmss}";
                        }

                        if (payload.Temperature.HasValue)
                        {
                            eventsToProcess.Add(new ProcessTelemetryEventCommand(
                                TenantId: payload.TenantId,
                                DeviceId: payload.DeviceId,
                                TelemetryKey: "temperature",
                                TelemetryValue: payload.Temperature.Value,
                                SourceEventId: $"TEMP-{eventIdBase}"
                            ));
                        }

                        if (payload.Pressure.HasValue)
                        {
                            eventsToProcess.Add(new ProcessTelemetryEventCommand(
                                TenantId: payload.TenantId,
                                DeviceId: payload.DeviceId,
                                TelemetryKey: "pressure",
                                TelemetryValue: payload.Pressure.Value,
                                SourceEventId: $"PRES-{eventIdBase}"
                            ));
                        }
                    }

                    foreach (var command in eventsToProcess)
                    {
                        // ─── Tüketici (Consumer) Seviyesinde Mükerrer Kayıt Filtreleme (Idempotency Layer) 🛡️ ───
                        using var scope = _serviceProvider.CreateScope();
                        var taskStore = scope.ServiceProvider.GetRequiredService<IWorkflowTaskStore>();

                        var isDuplicate = await taskStore.HasEventBeenProcessedAsync(command.TenantId, command.SourceEventId, cancellationToken);
                        if (isDuplicate)
                        {
                            _logger.LogInformation("⏭️ [KinesisConsumer] Mükerrer telemetri paketi elendi. Cihaz: {DeviceId}, Olay: {EventId}",
                                command.DeviceId, command.SourceEventId);
                            continue; // İşleme girmeden (SignalR ve MediatR tetiklemeden) atla!
                        }

                        // Canlı izleme paneli için telemetriyi SignalR üzerinden yayınla 📊⚡
                        if (_hubContext != null)
                        {
                            var groups = new[] { command.TenantId.ToString(), "demo-tenant" };
                            await _hubContext.Clients.Groups(groups).SendAsync(
                                "ReceiveTelemetry",
                                new
                                {
                                    DeviceId = command.DeviceId,
                                    TelemetryKey = command.TelemetryKey,
                                    TelemetryValue = command.TelemetryValue,
                                    Timestamp = DateTime.UtcNow
                                },
                                cancellationToken
                            );
                        }

                        const int maxRetryAttempts = 3;
                        int attempt = 0;

                        while (true)
                        {
                            try
                            {
                                attempt++;
                                var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

                                await mediator.Send(command, cancellationToken);
                                break; // Başarılı, döngüden çık
                            }
                            catch (Exception ex) when (attempt < maxRetryAttempts && !cancellationToken.IsCancellationRequested)
                            {
                                LogProcessRecordRetryWarning(ex, attempt, maxRetryAttempts);
                                await Task.Delay(attempt * 500, cancellationToken); // Artan bekleme süresi
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogProcessRecordError(ex, rawData);
            await SendToDeadLetterQueueAsync(rawData, ex.Message);
        }
    }

    private async Task SendToDeadLetterQueueAsync(string rawData, string errorMessage)
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(new
            {
                RawData = rawData,
                ErrorMessage = errorMessage,
                FailedAt = DateTime.UtcNow
            });

            var request = new SendMessageRequest
            {
                QueueUrl = _dlqQueueUrl,
                MessageBody = messageBody
            };

            if (_sqs != null)
            {
                await _sqs.SendMessageAsync(request);
                LogParkedToDlq();
            }
            else
            {
                LogSqsClientNotConfigured(rawData);
            }
        }
        catch (Exception dlqEx)
        {
            LogDlqCriticalError(dlqEx, rawData);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "⚠️ AWS Kinesis/SQS bağlantısı kurulamadı. Tüketici (Consumer) pasif modda bekletiliyor...")]
    private partial void LogAwsConnectionFailed();

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "[KinesisConsumer] AWS Kinesis pasif modda. Yeni veri bekleniyor...")]
    private partial void LogKinesisPassiveMode();

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "🚀 AWS Kinesis Telemetry Consumer başlatıldı. Stream: {Stream}")]
    private partial void LogConsumerStarted(string stream);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "❌ [KinesisConsumer] Okuma döngüsünde hata meydana geldi. 10 saniye sonra tekrar denenecek...")]
    private partial void LogReadLoopError(Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "❌ [KinesisConsumer] Kayıt işlenirken hata oluştu! DLQ'ya gönderiliyor. RawData: {Raw}")]
    private partial void LogProcessRecordError(Exception ex, string raw);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "🚨 [KinesisConsumer] Hatalı kayıt DLQ kuyruğuna (SQS) park edildi.")]
    private partial void LogParkedToDlq();

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "⚠️ [KinesisConsumer] AWS SQS istemcisi yapılandırılmamış, DLQ kaydı atlanıyor. RawData: {Raw}")]
    private partial void LogSqsClientNotConfigured(string raw);

    [LoggerMessage(EventId = 8, Level = LogLevel.Critical, Message = "💥 [KinesisConsumer] DLQ'ya mesaj gönderilirken kritik hata! Veri tamamen kaybolabilir. RawData: {Raw}")]
    private partial void LogDlqCriticalError(Exception ex, string raw);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "❌ [KinesisConsumer] Shard {ShardId} işlenirken hata oluştu!")]
    private partial void LogShardProcessError(Exception ex, string shardId);

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "⚠️ [KinesisConsumer] Kayıt işlenirken geçici hata oluştu. Deneme {Attempt}/{MaxRetryAttempts}. Yeniden deneniyor...")]
    private partial void LogProcessRecordRetryWarning(Exception ex, int attempt, int maxRetryAttempts);
}
