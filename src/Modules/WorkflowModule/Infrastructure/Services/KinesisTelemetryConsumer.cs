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
    private readonly Amazon.DynamoDBv2.IAmazonDynamoDB? _dynamoDb;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KinesisTelemetryConsumer> _logger;
    private readonly IHubContext<TelemetryHub>? _hubContext;
    private readonly string _streamName;
    private readonly string _dlqQueueUrl;
    private readonly bool _isEnabled;
    private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}";
    private readonly ConcurrentDictionary<string, (Task Task, CancellationTokenSource Cts)> _activeShardTasks = new();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public KinesisTelemetryConsumer(
        IAmazonKinesis? kinesis,
        IAmazonSQS? sqs,
        Amazon.DynamoDBv2.IAmazonDynamoDB? dynamoDb,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<KinesisTelemetryConsumer> logger,
        IHubContext<TelemetryHub>? hubContext = null)
    {
        _kinesis = kinesis;
        _sqs = sqs;
        _dynamoDb = dynamoDb;
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

    private bool _useBypass;
    private int _bypassPollCount = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _useBypass = _dynamoDb == null;

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
                    if (kvp.Value.Task.IsCompleted)
                    {
                        _activeShardTasks.TryRemove(kvp.Key, out _);
                    }
                }

                // DynamoDB bypass modundaysak bağlantıyı periyodik olarak kontrol etmeyi dene
                if (_useBypass && _dynamoDb != null)
                {
                    _bypassPollCount++;
                    if (_bypassPollCount >= 20) // Her 20 döngüde bir (yaklaşık 5 dakikada bir)
                    {
                        _logger.LogInformation("🔄 [LeaseManager] DynamoDB bağlantısı tekrar deneniyor...");
                        _useBypass = false;
                        _bypassPollCount = 0;
                    }
                }

                // 1. Shard'ları listele
                var describeRequest = new DescribeStreamRequest { StreamName = _streamName };
                var describeResponse = await _kinesis!.DescribeStreamAsync(describeRequest, stoppingToken);
                var shards = describeResponse.StreamDescription.Shards.ToList();

                // 2. Kilitleri dengele ve işlemleri yönet (Acquire, Renew, Steal, Stop)
                await AcquireRenewOrBalanceLeasesAsync(shards, stoppingToken);
            }
            catch (Exception ex)
            {
                LogReadLoopError(ex);
            }

            try
            {
                // Shard listesini güncellemek ve kilitleri tazelemek için bekleme (örn. 15 saniye)
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
            await Task.WhenAll(_activeShardTasks.Values.Select(v => v.Task));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tüketici durdurulurken bazı shard işlemleri hata fırlattı.");
        }

        // Kapatılırken kilitleri serbest bırak (sadece sahipliği UNOWNED yaparak checkpoint'leri koru)
        await ReleaseLeasesAsync();
    }

    private async Task ProcessShardAsync(string shardId, CancellationToken cancellationToken)
    {
        try
        {
            var checkpoint = await GetCheckpointAsync(shardId, cancellationToken);
            GetShardIteratorRequest iteratorRequest;

            if (!string.IsNullOrEmpty(checkpoint))
            {
                _logger.LogInformation("💾 [KinesisConsumer] Shard {ShardId} için checkpoint bulundu. Kaldığı yer: {Checkpoint}", shardId, checkpoint);
                iteratorRequest = new GetShardIteratorRequest
                {
                    StreamName = _streamName,
                    ShardId = shardId,
                    ShardIteratorType = ShardIteratorType.AFTER_SEQUENCE_NUMBER,
                    StartingSequenceNumber = checkpoint
                };
            }
            else
            {
                // Konfigürasyondan varsayılan iterator tipini al (canlıda TRIM_HORIZON, localstack'te LATEST gibi)
                var defaultIteratorTypeStr = _serviceProvider.GetRequiredService<IConfiguration>()
                    .GetValue<string>("AWS:Kinesis:ShardIteratorType") ?? "TRIM_HORIZON";

                var defaultIteratorType = ShardIteratorType.FindValue(defaultIteratorTypeStr);

                _logger.LogInformation("🆕 [KinesisConsumer] Shard {ShardId} için checkpoint bulunamadı. Varsayılan iterator ile başlanıyor: {IteratorType}", 
                    shardId, defaultIteratorType.Value);

                iteratorRequest = new GetShardIteratorRequest
                {
                    StreamName = _streamName,
                    ShardId = shardId,
                    ShardIteratorType = defaultIteratorType
                };
            }

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
                
                if (getRecordsResponse.Records.Count > 0)
                {
                    foreach (var record in getRecordsResponse.Records)
                    {
                        await ProcessRecordWithRetryAsync(record, cancellationToken);
                    }

                    // Başarılı bir şekilde işlenen son kaydın sequence numarasını checkpoint olarak kaydet
                    var lastRecord = getRecordsResponse.Records[^1];
                    await SaveCheckpointAsync(shardId, lastRecord.SequenceNumber, cancellationToken);
                }

                iterator = getRecordsResponse.NextShardIterator;
                
                // Kinesis rate limit'i aşmamak için bekleme (her shard için 1 saniye)
                await Task.Delay(1000, cancellationToken);
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
            if (_activeShardTasks.TryRemove(shardId, out var item))
            {
                item.Cts.Dispose();
            }
        }
    }

    private async Task ProcessRecordWithRetryAsync(Amazon.Kinesis.Model.Record record, CancellationToken cancellationToken)
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
                            var tenantGroup = payload.TenantId != Guid.Empty ? $"TENANT_GROUP_{payload.TenantId}" : "demo-tenant";
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
                            var tenantGroup = payload.TenantId != Guid.Empty ? $"TENANT_GROUP_{payload.TenantId}" : "demo-tenant";
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
                            var groups = new[] { $"TENANT_GROUP_{command.TenantId}", "demo-tenant" };
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

    private class LeaseRecord
    {
        public string ShardId { get; set; } = null!;
        public string? LeaseOwner { get; set; }
        public long LeaseExpiration { get; set; }
        public int Version { get; set; }
        public string? SequenceNumber { get; set; }
    }

    private async Task<List<LeaseRecord>> GetAllLeasesAsync(CancellationToken cancellationToken)
    {
        var list = new List<LeaseRecord>();
        if (_useBypass || _dynamoDb == null) return list;

        var pk = $"KINESIS_LEASE#{_streamName}";

        try
        {
            var queryRequest = new Amazon.DynamoDBv2.Model.QueryRequest
            {
                TableName = "OmniPulse_WorkflowTasks",
                KeyConditionExpression = "PK = :pk",
                ExpressionAttributeValues = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { ":pk", new Amazon.DynamoDBv2.Model.AttributeValue { S = pk } }
                },
                ConsistentRead = true
            };

            var response = await _dynamoDb.QueryAsync(queryRequest, cancellationToken);
            foreach (var item in response.Items)
            {
                var sk = item.GetValueOrDefault("SK")?.S ?? "";
                if (!sk.StartsWith("SHARD#")) continue;

                var shardId = sk.Substring("SHARD#".Length);
                var owner = item.GetValueOrDefault("LeaseOwner")?.S;
                var expStr = item.GetValueOrDefault("LeaseExpiration")?.N;
                var versionStr = item.GetValueOrDefault("Version")?.N;
                var seqStr = item.GetValueOrDefault("SequenceNumber")?.S;

                long.TryParse(expStr, out var expiration);
                int.TryParse(versionStr, out var version);

                list.Add(new LeaseRecord
                {
                    ShardId = shardId,
                    LeaseOwner = owner,
                    LeaseExpiration = expiration,
                    Version = version,
                    SequenceNumber = seqStr
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [LeaseManager] DynamoDB bağlantısı kurulamadı. Shard kilit yönetimi geçici olarak atlanıyor (Local/Bypass Modu).");
            _useBypass = true;
        }

        return list;
    }

    private async Task TryInitializeLeaseAsync(string shardId, CancellationToken cancellationToken)
    {
        if (_useBypass || _dynamoDb == null) return;

        var pk = $"KINESIS_LEASE#{_streamName}";
        var sk = $"SHARD#{shardId}";

        try
        {
            var putRequest = new Amazon.DynamoDBv2.Model.PutItemRequest
            {
                TableName = "OmniPulse_WorkflowTasks",
                Item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = pk } },
                    { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = sk } },
                    { "LeaseOwner", new Amazon.DynamoDBv2.Model.AttributeValue { S = "UNOWNED" } },
                    { "LeaseExpiration", new Amazon.DynamoDBv2.Model.AttributeValue { N = "0" } },
                    { "Version", new Amazon.DynamoDBv2.Model.AttributeValue { N = "1" } }
                },
                ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
            };

            await _dynamoDb.PutItemAsync(putRequest, cancellationToken);
            _logger.LogInformation("🔑 [LeaseManager] Shard {ShardId} için kilit kaydı ilk kez oluşturuldu.", shardId);
        }
        catch (Amazon.DynamoDBv2.Model.ConditionalCheckFailedException)
        {
            // Zaten oluşturulmuş, sorun değil
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [LeaseManager] Shard {ShardId} için kilit kaydı oluşturulurken hata.", shardId);
        }
    }

    private async Task AcquireRenewOrBalanceLeasesAsync(List<Shard> shards, CancellationToken cancellationToken)
    {
        var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // 1. DynamoDB'deki tüm mevcut kilit kayıtlarını oku
        var leases = await GetAllLeasesAsync(cancellationToken);
        
        // 2. Kinesis'ten gelen ama DynamoDB'de kaydı olmayan shard'lar varsa bunları başlat
        var leaseMap = leases.ToDictionary(l => l.ShardId);
        foreach (var shard in shards)
        {
            if (!leaseMap.ContainsKey(shard.ShardId))
            {
                await TryInitializeLeaseAsync(shard.ShardId, cancellationToken);
                var newLease = new LeaseRecord
                {
                    ShardId = shard.ShardId,
                    LeaseOwner = "UNOWNED",
                    LeaseExpiration = 0,
                    Version = 1
                };
                leases.Add(newLease);
                leaseMap[shard.ShardId] = newLease;
            }
        }

        // 3. Aktif worker'ları belirle
        var activeWorkers = leases
            .Where(l => l.LeaseOwner != null && l.LeaseOwner != "UNOWNED" && l.LeaseExpiration >= nowSeconds)
            .Select(l => l.LeaseOwner!)
            .Distinct()
            .ToList();

        if (!activeWorkers.Contains(_workerId))
        {
            activeWorkers.Add(_workerId);
        }

        int n = shards.Count;
        int m = activeWorkers.Count;
        int targetCount = (int)Math.Ceiling((double)n / m);

        _logger.LogInformation("📊 [LeaseBalancer] Toplam Shard Sayısı: {ShardCount}, Aktif Worker Sayısı: {WorkerCount}, Hedef Shard/Worker: {TargetCount}",
            n, m, targetCount);

        // 4. Kendi kilitlerimizi belirle
        var myLeases = leases.Where(l => l.LeaseOwner == _workerId).ToList();
        
        // 5. Boştaki veya süresi dolmuş kilitleri belirle
        var availableLeases = leases
            .Where(l => l.LeaseOwner == "UNOWNED" || l.LeaseExpiration < nowSeconds)
            .ToList();

        // 6. Diğer worker'ların kilit sayılarını hesapla
        var workerLeaseCounts = activeWorkers
            .ToDictionary(w => w, w => leases.Count(l => l.LeaseOwner == w && l.LeaseExpiration >= nowSeconds));

        // Adım A: Kendi kilitlerimizi uzat (Renew)
        foreach (var lease in myLeases)
        {
            var success = await TryRenewLeaseInternalAsync(lease, cancellationToken);
            if (success)
            {
                if (!_activeShardTasks.ContainsKey(lease.ShardId))
                {
                    StartShardProcessing(lease.ShardId, cancellationToken);
                }
            }
            else
            {
                StopShardProcessing(lease.ShardId);
            }
        }

        var activeMyLeases = leases.Where(l => l.LeaseOwner == _workerId && l.LeaseExpiration >= nowSeconds).ToList();
        int myCount = activeMyLeases.Count;

        // Adım B: Eğer hedef kilit sayımızın altındaysak, boşta/süresi dolmuş kilitleri al
        if (myCount < targetCount && availableLeases.Count > 0)
        {
            foreach (var lease in availableLeases)
            {
                if (myCount >= targetCount) break;

                var success = await TryAcquireExpiredLeaseInternalAsync(lease, cancellationToken);
                if (success)
                {
                    myCount++;
                    StartShardProcessing(lease.ShardId, cancellationToken);
                }
            }
        }

        // Adım C: Hâlâ hedef kilit sayımızın altındaysak ve boşta kilit kalmadıysa, en çok kilidi olan worker'dan çal!
        if (myCount < targetCount)
        {
            var overAllocatedWorkers = workerLeaseCounts
                .Where(kvp => kvp.Key != _workerId && kvp.Value > targetCount)
                .OrderByDescending(kvp => kvp.Value)
                .ToList();

            if (overAllocatedWorkers.Count > 0)
            {
                var targetWorker = overAllocatedWorkers[0].Key;
                var targetLease = leases.FirstOrDefault(l => l.LeaseOwner == targetWorker && l.LeaseExpiration >= nowSeconds);
                if (targetLease != null)
                {
                    _logger.LogInformation("🏴‍☠️ [LeaseBalancer] Bizde {MyCount} kilit var, Hedef {TargetCount}. {TargetWorker} üzerinde {TheirCount} kilit var. Shard {ShardId} kilidini çalmaya çalışıyoruz...",
                        myCount, targetCount, targetWorker, overAllocatedWorkers[0].Value, targetLease.ShardId);

                    var success = await TryStealLeaseInternalAsync(targetLease, targetWorker, cancellationToken);
                    if (success)
                    {
                        StartShardProcessing(targetLease.ShardId, cancellationToken);
                    }
                }
            }
        }

        // Adım D: Bizim aktif olarak işlediğimiz ama kilit listesinde başkasına geçmiş veya süresi dolmuş shard'ları durdur (Emniyet kemeri)
        var currentlyOwnedShardIds = leases
            .Where(l => l.LeaseOwner == _workerId && l.LeaseExpiration >= nowSeconds)
            .Select(l => l.ShardId)
            .ToHashSet();

        foreach (var activeShardId in _activeShardTasks.Keys)
        {
            if (!currentlyOwnedShardIds.Contains(activeShardId))
            {
                _logger.LogWarning("⚠️ [LeaseBalancer] Shard {ShardId} kilit sahipliği doğrulanmadı, işleme durduruluyor.", activeShardId);
                StopShardProcessing(activeShardId);
            }
        }
    }

    private async Task<bool> TryRenewLeaseInternalAsync(LeaseRecord lease, CancellationToken cancellationToken)
    {
        if (_useBypass || _dynamoDb == null) return true;

        var pk = $"KINESIS_LEASE#{_streamName}";
        var sk = $"SHARD#{lease.ShardId}";
        var leaseDurationSeconds = 30;
        var expirationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + leaseDurationSeconds;

        try
        {
            var updateRequest = new Amazon.DynamoDBv2.Model.UpdateItemRequest
            {
                TableName = "OmniPulse_WorkflowTasks",
                Key = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = pk } },
                    { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = sk } }
                },
                UpdateExpression = "SET LeaseExpiration = :exp, Version = :newVersion",
                ConditionExpression = "Version = :expectedVersion AND LeaseOwner = :owner",
                ExpressionAttributeValues = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { ":exp", new Amazon.DynamoDBv2.Model.AttributeValue { N = expirationTime.ToString() } },
                    { ":newVersion", new Amazon.DynamoDBv2.Model.AttributeValue { N = (lease.Version + 1).ToString() } },
                    { ":expectedVersion", new Amazon.DynamoDBv2.Model.AttributeValue { N = lease.Version.ToString() } },
                    { ":owner", new Amazon.DynamoDBv2.Model.AttributeValue { S = _workerId } }
                }
            };

            await _dynamoDb.UpdateItemAsync(updateRequest, cancellationToken);
            lease.LeaseExpiration = expirationTime;
            lease.Version++;
            _logger.LogDebug("🔄 [LeaseBalancer] Shard {ShardId} kilidi uzatıldı.", lease.ShardId);
            return true;
        }
        catch (Amazon.DynamoDBv2.Model.ConditionalCheckFailedException)
        {
            _logger.LogWarning("⚠️ [LeaseBalancer] Shard {ShardId} kilidi uzatılamadı (yarış durumu/sahiplik değişti).", lease.ShardId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [LeaseBalancer] Shard {ShardId} kilidi uzatılırken beklenmeyen hata.", lease.ShardId);
            return false;
        }
    }

    private async Task<bool> TryAcquireExpiredLeaseInternalAsync(LeaseRecord lease, CancellationToken cancellationToken)
    {
        if (_useBypass || _dynamoDb == null) return true;

        var pk = $"KINESIS_LEASE#{_streamName}";
        var sk = $"SHARD#{lease.ShardId}";
        var leaseDurationSeconds = 30;
        var expirationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + leaseDurationSeconds;

        try
        {
            var updateRequest = new Amazon.DynamoDBv2.Model.UpdateItemRequest
            {
                TableName = "OmniPulse_WorkflowTasks",
                Key = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = pk } },
                    { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = sk } }
                },
                UpdateExpression = "SET LeaseOwner = :owner, LeaseExpiration = :exp, Version = :newVersion",
                ConditionExpression = "Version = :expectedVersion",
                ExpressionAttributeValues = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { ":owner", new Amazon.DynamoDBv2.Model.AttributeValue { S = _workerId } },
                    { ":exp", new Amazon.DynamoDBv2.Model.AttributeValue { N = expirationTime.ToString() } },
                    { ":newVersion", new Amazon.DynamoDBv2.Model.AttributeValue { N = (lease.Version + 1).ToString() } },
                    { ":expectedVersion", new Amazon.DynamoDBv2.Model.AttributeValue { N = lease.Version.ToString() } }
                }
            };

            await _dynamoDb.UpdateItemAsync(updateRequest, cancellationToken);
            lease.LeaseOwner = _workerId;
            lease.LeaseExpiration = expirationTime;
            lease.Version++;
            _logger.LogInformation("🔑 [LeaseBalancer] Shard {ShardId} kilidi boşta/süresi dolmuş olduğu için alındı.", lease.ShardId);
            return true;
        }
        catch (Amazon.DynamoDBv2.Model.ConditionalCheckFailedException)
        {
            _logger.LogWarning("⚠️ [LeaseBalancer] Shard {ShardId} kilidi alınamadı (başka bir worker bizden önce davrandı).", lease.ShardId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [LeaseBalancer] Shard {ShardId} kilidi alınırken beklenmeyen hata.", lease.ShardId);
            return false;
        }
    }

    private async Task<bool> TryStealLeaseInternalAsync(LeaseRecord lease, string currentOwner, CancellationToken cancellationToken)
    {
        if (_useBypass || _dynamoDb == null) return true;

        var pk = $"KINESIS_LEASE#{_streamName}";
        var sk = $"SHARD#{lease.ShardId}";
        var leaseDurationSeconds = 30;
        var expirationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + leaseDurationSeconds;

        try
        {
            var updateRequest = new Amazon.DynamoDBv2.Model.UpdateItemRequest
            {
                TableName = "OmniPulse_WorkflowTasks",
                Key = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = pk } },
                    { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = sk } }
                },
                UpdateExpression = "SET LeaseOwner = :owner, LeaseExpiration = :exp, Version = :newVersion",
                ConditionExpression = "Version = :expectedVersion AND LeaseOwner = :expectedOwner",
                ExpressionAttributeValues = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { ":owner", new Amazon.DynamoDBv2.Model.AttributeValue { S = _workerId } },
                    { ":exp", new Amazon.DynamoDBv2.Model.AttributeValue { N = expirationTime.ToString() } },
                    { ":newVersion", new Amazon.DynamoDBv2.Model.AttributeValue { N = (lease.Version + 1).ToString() } },
                    { ":expectedVersion", new Amazon.DynamoDBv2.Model.AttributeValue { N = lease.Version.ToString() } },
                    { ":expectedOwner", new Amazon.DynamoDBv2.Model.AttributeValue { S = currentOwner } }
                }
            };

            await _dynamoDb.UpdateItemAsync(updateRequest, cancellationToken);
            lease.LeaseOwner = _workerId;
            lease.LeaseExpiration = expirationTime;
            lease.Version++;
            _logger.LogWarning("🏴‍☠️ [LeaseBalancer] Shard {ShardId} kilidi dengelenme amacıyla {OldOwner} kullanıcısından ÇALINDI.",
                lease.ShardId, currentOwner);
            return true;
        }
        catch (Amazon.DynamoDBv2.Model.ConditionalCheckFailedException)
        {
            _logger.LogWarning("⚠️ [LeaseBalancer] Shard {ShardId} kilidi çalınamadı (versiyon veya sahip değişmiş).", lease.ShardId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [LeaseBalancer] Shard {ShardId} kilidi çalınırken beklenmeyen hata.", lease.ShardId);
            return false;
        }
    }

    private void StartShardProcessing(string shardId, CancellationToken cancellationToken)
    {
        if (!_activeShardTasks.ContainsKey(shardId))
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var task = Task.Run(() => ProcessShardAsync(shardId, cts.Token), cts.Token);
            if (_activeShardTasks.TryAdd(shardId, (task, cts)))
            {
                _logger.LogInformation("🚀 [KinesisConsumer] Shard {ShardId} için okuma görevi başlatıldı.", shardId);
            }
            else
            {
                cts.Dispose();
            }
        }
    }

    private void StopShardProcessing(string shardId)
    {
        if (_activeShardTasks.TryRemove(shardId, out var item))
        {
            _logger.LogWarning("⚠️ [KinesisConsumer] Shard {ShardId} okuma görevi durduruluyor...", shardId);
            item.Cts.Cancel();
            item.Cts.Dispose();
        }
    }

    private async Task<string?> GetCheckpointAsync(string shardId, CancellationToken cancellationToken)
    {
        if (_useBypass || _dynamoDb == null) return null;

        var pk = $"KINESIS_LEASE#{_streamName}";
        var sk = $"SHARD#{shardId}";

        try
        {
            var getRequest = new Amazon.DynamoDBv2.Model.GetItemRequest
            {
                TableName = "OmniPulse_WorkflowTasks",
                Key = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = pk } },
                    { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = sk } }
                },
                ConsistentRead = true
            };

            var response = await _dynamoDb.GetItemAsync(getRequest, cancellationToken);
            if (response.Item != null && response.Item.TryGetValue("SequenceNumber", out var seqVal))
            {
                return seqVal.S;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [CheckpointManager] Shard {ShardId} için checkpoint sorgulanırken hata oluştu.", shardId);
        }

        return null;
    }

    private async Task SaveCheckpointAsync(string shardId, string sequenceNumber, CancellationToken cancellationToken)
    {
        if (_useBypass || _dynamoDb == null) return;

        var pk = $"KINESIS_LEASE#{_streamName}";
        var sk = $"SHARD#{shardId}";

        try
        {
            var updateRequest = new Amazon.DynamoDBv2.Model.UpdateItemRequest
            {
                TableName = "OmniPulse_WorkflowTasks",
                Key = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = pk } },
                    { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = sk } }
                },
                UpdateExpression = "SET SequenceNumber = :seq",
                ConditionExpression = "LeaseOwner = :owner",
                ExpressionAttributeValues = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { ":seq", new Amazon.DynamoDBv2.Model.AttributeValue { S = sequenceNumber } },
                    { ":owner", new Amazon.DynamoDBv2.Model.AttributeValue { S = _workerId } }
                }
            };

            await _dynamoDb.UpdateItemAsync(updateRequest, cancellationToken);
            _logger.LogDebug("💾 [CheckpointManager] Shard {ShardId} için checkpoint kaydedildi: {SequenceNumber}", shardId, sequenceNumber);
        }
        catch (Amazon.DynamoDBv2.Model.ConditionalCheckFailedException)
        {
            _logger.LogWarning("⚠️ [CheckpointManager] Shard {ShardId} için checkpoint kaydedilemedi, kilit kaybedilmiş olabilir.", shardId);
            StopShardProcessing(shardId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [CheckpointManager] Shard {ShardId} için checkpoint kaydedilirken hata oluştu.", shardId);
        }
    }

    private async Task ReleaseLeasesAsync()
    {
        if (_useBypass || _dynamoDb == null) return;

        _logger.LogInformation("🔌 [LeaseManager] Kapatılıyor, sahiplenilen kilitler serbest bırakılıyor...");

        foreach (var shardId in _activeShardTasks.Keys)
        {
            var pk = $"KINESIS_LEASE#{_streamName}";
            var sk = $"SHARD#{shardId}";

            try
            {
                var updateRequest = new Amazon.DynamoDBv2.Model.UpdateItemRequest
                {
                    TableName = "OmniPulse_WorkflowTasks",
                    Key = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                    {
                        { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = pk } },
                        { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = sk } }
                    },
                    UpdateExpression = "SET LeaseOwner = :unowned, LeaseExpiration = :zero",
                    ConditionExpression = "LeaseOwner = :owner",
                    ExpressionAttributeValues = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                    {
                        { ":unowned", new Amazon.DynamoDBv2.Model.AttributeValue { S = "UNOWNED" } },
                        { ":zero", new Amazon.DynamoDBv2.Model.AttributeValue { N = "0" } },
                        { ":owner", new Amazon.DynamoDBv2.Model.AttributeValue { S = _workerId } }
                    }
                };

                await _dynamoDb.UpdateItemAsync(updateRequest);
                _logger.LogInformation("🔓 [LeaseManager] Shard {ShardId} kilidi serbest bırakıldı.", shardId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [LeaseManager] Shard {ShardId} kilidi serbest bırakılırken hata oluştu.", shardId);
            }
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
