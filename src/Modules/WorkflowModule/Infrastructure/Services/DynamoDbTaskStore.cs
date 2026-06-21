using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using OmniPulse.Modules.WorkflowModule.Domain.Entities;

namespace OmniPulse.Modules.WorkflowModule.Infrastructure.Services;

public class DynamoDbTaskStore : IWorkflowTaskStore
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ILogger<DynamoDbTaskStore> _logger;
    private const string TableName = "OmniPulse_WorkflowTasks";

    // Geliştirici Dostu Fallback: Eğer AWS servisleri bağlı değilse yerel hafızada tutarız! 🛠️
    private static readonly Dictionary<string, (WorkflowTask Task, int Version)> İnMemoryFallback = new();
    private static readonly Lock Lock = new();
    private bool _useFallback;
    private bool _tableVerified;
    private DateTime _lastCheckTimeUtc = DateTime.MinValue;
    private readonly TimeSpan _checkCooldown = TimeSpan.FromSeconds(30);

    public DynamoDbTaskStore(IAmazonDynamoDB dynamoDb, ILogger<DynamoDbTaskStore> logger)
    {
        _dynamoDb = dynamoDb;
        _logger = logger;
        
        // AWS Credential'ları veya AWS Region ayarlanmamışsa otomatik fallback'e geç
        try
        {
            var config = _dynamoDb.Config;
            _useFallback = config == null || (string.IsNullOrEmpty(config.RegionEndpoint?.SystemName) && string.IsNullOrEmpty(config.ServiceURL));
        }
        catch
        {
            _useFallback = true;
        }

        if (_useFallback)
        {
            _logger.LogWarning("⚠️ AWS DynamoDB bağlantısı kurulamadı. Geliştirici modu için yerel bellek (In-Memory) task deposu aktif edildi!");
        }
    }

    private async Task EnsureTableExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _dynamoDb.DescribeTableAsync(TableName, cancellationToken);
            if (response.Table != null)
            {
                return;
            }
        }
        catch (ResourceNotFoundException)
        {
            _logger.LogInformation("⚠️ '{TableName}' tablosu bulunamadı. GSI (TenantStatusIndex) ile yeni tablo oluşturuluyor...", TableName);
            var createRequest = new CreateTableRequest
            {
                TableName = TableName,
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition("PK", ScalarAttributeType.S),
                    new AttributeDefinition("SK", ScalarAttributeType.S),
                    new AttributeDefinition("TenantId", ScalarAttributeType.S),
                    new AttributeDefinition("Status", ScalarAttributeType.S)
                },
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("PK", KeyType.HASH),
                    new KeySchemaElement("SK", KeyType.RANGE)
                },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "TenantStatusIndex",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement("TenantId", KeyType.HASH),
                            new KeySchemaElement("Status", KeyType.RANGE)
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL }
                    }
                }
            };
            await _dynamoDb.CreateTableAsync(createRequest, cancellationToken);
            _logger.LogInformation("✅ '{TableName}' tablosu ve 'TenantStatusIndex' GSI başarıyla oluşturuldu!", TableName);
            
            // Tablo ACTIVE olana kadar kısa bir süre bekle (özellikle LocalStack için)
            int retries = 0;
            while (retries < 10)
            {
                var desc = await _dynamoDb.DescribeTableAsync(TableName, cancellationToken);
                if (desc.Table.TableStatus == TableStatus.ACTIVE)
                    break;
                await Task.Delay(500, cancellationToken);
                retries++;
            }
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_useFallback)
        {
            await TryRecoverAwsConnectionAsync(cancellationToken);
            return;
        }

        if (!_tableVerified)
        {
            try
            {
                await EnsureTableExistsAsync(cancellationToken);
                _tableVerified = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ AWS DynamoDB tablosu doğrulanamadı veya oluşturulamadı. Bellek moduna (Fallback) geçiliyor.");
                lock (Lock)
                {
                    _useFallback = true;
                }
            }
        }
    }

    private async Task PutTaskToDynamoDbAsync(WorkflowTask task, CancellationToken cancellationToken = default)
    {
        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = task.PK } },
                { "SK", new AttributeValue { S = task.SK } },
                { "TenantId", new AttributeValue { S = task.TenantId.ToString() } },
                { "TaskId", new AttributeValue { S = task.TaskId.ToString() } },
                { "WorkflowDefinitionId", new AttributeValue { S = task.WorkflowDefinitionId.ToString() } },
                { "WorkflowName", new AttributeValue { S = task.WorkflowName } },
                { "AssetId", new AttributeValue { S = task.AssetId.ToString() } },
                { "AssetName", new AttributeValue { S = task.AssetName } },
                { "AssetType", new AttributeValue { S = task.AssetType } },
                { "Status", new AttributeValue { S = task.Status } },
                { "Description", new AttributeValue { S = task.Description } },
                { "SourceEventId", new AttributeValue { S = task.SourceEventId } },
                { "CreatedAtUtc", new AttributeValue { S = task.CreatedAtUtc.ToString("o") } },
                { "UpdatedAtUtc", new AttributeValue { S = task.UpdatedAtUtc.ToString("o") } },
                { "Version", new AttributeValue { N = (task.Version + 1).ToString() } }
            }
        };

        if (task.AssignedUserId.HasValue)
        {
            request.Item.Add("AssignedUserId", new AttributeValue { S = task.AssignedUserId.Value.ToString() });
        }

        if (!string.IsNullOrEmpty(task.ExecutionContextJson))
        {
            request.Item.Add("ExecutionContextJson", new AttributeValue { S = task.ExecutionContextJson });
        }

        if (task.Version == 1)
        {
            request.ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)";
        }
        else
        {
            request.ConditionExpression = "Version = :expectedVersion";
            request.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":expectedVersion", new AttributeValue { N = task.Version.ToString() } }
            };
        }

        await _dynamoDb.PutItemAsync(request, cancellationToken);
        task.Version++;
    }

    private async Task TryRecoverAwsConnectionAsync(CancellationToken cancellationToken)
    {
        if (!_useFallback) return;

        if (DateTime.UtcNow - _lastCheckTimeUtc < _checkCooldown) return;

        _lastCheckTimeUtc = DateTime.UtcNow;
        _logger.LogInformation("🔄 AWS DynamoDB bağlantısı kontrol ediliyor...");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            
            // Tablo yoksa oluştur, varsa geç
            await EnsureTableExistsAsync(cts.Token);
            
            _logger.LogInformation("🔄 AWS bağlantısı geri geldi! Bellekteki veriler DynamoDB'ye aktarılıyor...");

            List<WorkflowTask> tasksToSync;
            lock (Lock)
            {
                tasksToSync = İnMemoryFallback.Values.Select(v => v.Task).ToList();
            }

            foreach (var task in tasksToSync)
            {
                await PutTaskToDynamoDbAsync(task, cancellationToken);
            }

            lock (Lock)
            {
                İnMemoryFallback.Clear();
                _useFallback = false;
                _tableVerified = true;
            }

            _logger.LogInformation("✅ Bellek tamamen eritildi ve AWS'ye aktarıldı. Sistem normal moda döndü!");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ AWS bağlantı kontrolü veya tablo oluşturma başarısız. Fallback moduna devam ediliyor.");
        }
    }

    public async Task SaveTaskAsync(WorkflowTask task, CancellationToken cancellationToken = default)
    {
        task.UpdatedAtUtc = DateTime.UtcNow;

        await EnsureInitializedAsync(cancellationToken);

        if (!_useFallback)
        {
            try
            {
                await PutTaskToDynamoDbAsync(task, cancellationToken);
                return;
            }
            catch (ConditionalCheckFailedException ex)
            {
                _logger.LogError(ex, "Yarış Durumu hatası: Task versiyonu veya anahtarı çakıştı!");
                throw new InvalidOperationException("Bu görev başka bir işlem tarafından güncellenmiş. Lütfen sayfayı yenileyip tekrar deneyin.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ AWS DynamoDB bağlantı hatası oluştu. In-Memory fallback aktif ediliyor.");
                lock (Lock)
                {
                    _useFallback = true;
                }
            }
        }

        // In-Memory Fallback
        lock (Lock)
        {
            var key = $"{task.PK}#{task.SK}";
            if (İnMemoryFallback.TryGetValue(key, out var existing))
            {
                // Concurrency (Yarış Durumu) Kontrolü
                if (existing.Version != task.Version)
                    throw new AmazonDynamoDBException($"Yarış Durumu (Race Condition) Algılandı! Task versiyonu uyuşmuyor. Beklenen: {existing.Version}, Gelen: {task.Version}");
                task.Version++;
                İnMemoryFallback[key] = (task, task.Version);
            }
            else
            {
                // Yeni kayıt
                task.Version = 1;
                İnMemoryFallback[key] = (task, 1);
            }
        }
        await Task.CompletedTask;
    }

    public async Task<WorkflowTask?> GetTaskAsync(Guid tenantId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var pk = $"TENANT#{tenantId}";
        var sk = $"TASK#{taskId}";

        await EnsureInitializedAsync(cancellationToken);

        if (!_useFallback)
        {
            try
            {
                var request = new GetItemRequest
                {
                    TableName = TableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "PK", new AttributeValue { S = pk } },
                        { "SK", new AttributeValue { S = sk } }
                    }
                };

                var response = await _dynamoDb.GetItemAsync(request, cancellationToken);
                if (response.Item == null || response.Item.Count == 0)
                {
                    return null;
                }

                return MapFromAttributes(response.Item);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ AWS DynamoDB bağlantı hatası oluştu. In-Memory fallback aktif ediliyor.");
                lock (Lock)
                {
                    _useFallback = true;
                }
            }
        }

        lock (Lock)
        {
            var key = $"{pk}#{sk}";
            return İnMemoryFallback.TryGetValue(key, out var val) ? val.Task : null;
        }
    }

    public async Task<bool> HasEventBeenProcessedAsync(Guid tenantId, string sourceEventId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (!_useFallback)
        {
            try
            {
                // Idempotency: Kaynak olayın bu tenant için daha önce işlenip işlenmediğini sorguluyoruz.
                // Query kullanarak PK bazlı filtre uygulayabiliriz (yüksek performans için).
                var request = new QueryRequest
                {
                    TableName = TableName,
                    KeyConditionExpression = "PK = :pk",
                    FilterExpression = "SourceEventId = :eventId",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":pk", new AttributeValue { S = $"TENANT#{tenantId}" } },
                        { ":eventId", new AttributeValue { S = sourceEventId } }
                    }
                };

                var response = await _dynamoDb.QueryAsync(request, cancellationToken);
                return response.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ AWS DynamoDB bağlantı hatası oluştu. In-Memory fallback aktif ediliyor.");
                lock (Lock)
                {
                    _useFallback = true;
                }
            }
        }

        lock (Lock)
        {
            return İnMemoryFallback.Values.Any(v => v.Task.TenantId == tenantId && v.Task.SourceEventId == sourceEventId);
        }
    }

    public async Task<IEnumerable<WorkflowTask>> GetTasksByTenantAsync(Guid tenantId, string? status = null, CancellationToken cancellationToken = default)
    {
        var pk = $"TENANT#{tenantId}";

        await EnsureInitializedAsync(cancellationToken);

        if (!_useFallback)
        {
            try
            {
                QueryRequest request;
                if (!string.IsNullOrEmpty(status))
                {
                    // TenantStatusIndex GSI kullanımı - RCU ve WCU maliyetlerini minimize eder! 🚀
                    request = new QueryRequest
                    {
                        TableName = TableName,
                        IndexName = "TenantStatusIndex",
                        KeyConditionExpression = "TenantId = :tenantId AND #s = :status",
                        ExpressionAttributeNames = new Dictionary<string, string> { { "#s", "Status" } },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            { ":tenantId", new AttributeValue { S = tenantId.ToString() } },
                            { ":status", new AttributeValue { S = status } }
                        }
                    };
                }
                else
                {
                    request = new QueryRequest
                    {
                        TableName = TableName,
                        KeyConditionExpression = "PK = :pk",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            { ":pk", new AttributeValue { S = pk } }
                        }
                    };
                }

                var response = await _dynamoDb.QueryAsync(request, cancellationToken);
                return response.Items.Select(MapFromAttributes).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ AWS DynamoDB bağlantı hatası oluştu. In-Memory fallback aktif ediliyor.");
                lock (Lock)
                {
                    _useFallback = true;
                }
            }
        }

        lock (Lock)
        {
            var query = İnMemoryFallback.Values
                .Where(v => v.Task.PK == pk);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(v => v.Task.Status == status);
            }

            return query.Select(v => v.Task).ToList();
        }
    }

    public async Task<bool> HasActiveTaskAsync(Guid tenantId, Guid assetId, Guid workflowDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var pk = $"TENANT#{tenantId}";

        await EnsureInitializedAsync(cancellationToken);

        if (!_useFallback)
        {
            try
            {
                var request = new QueryRequest
                {
                    TableName = TableName,
                    KeyConditionExpression = "PK = :pk",
                    FilterExpression = "AssetId = :assetId AND WorkflowDefinitionId = :wfId AND (#s = :pending OR #s = :inprogress)",
                    ExpressionAttributeNames = new Dictionary<string, string> { { "#s", "Status" } },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":pk",         new AttributeValue { S = pk } },
                        { ":assetId",    new AttributeValue { S = assetId.ToString() } },
                        { ":wfId",       new AttributeValue { S = workflowDefinitionId.ToString() } },
                        { ":pending",    new AttributeValue { S = "Pending" } },
                        { ":inprogress", new AttributeValue { S = "InProgress" } }
                    }
                };

                var response = await _dynamoDb.QueryAsync(request, cancellationToken);
                return response.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ DynamoDB bağlantı hatası. In-Memory fallback aktif.");
                lock (Lock)
                {
                    _useFallback = true;
                }
            }
        }

        lock (Lock)
        {
            return İnMemoryFallback.Values.Any(v =>
                v.Task.TenantId == tenantId &&
                v.Task.AssetId == assetId &&
                v.Task.WorkflowDefinitionId == workflowDefinitionId &&
                v.Task.Status is "Pending" or "InProgress");
        }
    }

    public async Task UpdateTaskStatusAsync(WorkflowTask task, string newStatus, CancellationToken cancellationToken = default)
    {
        var oldStatus = task.Status;
        task.Status = newStatus;
        task.UpdatedAtUtc = DateTime.UtcNow;

        await EnsureInitializedAsync(cancellationToken);

        if (!_useFallback)
        {
            try
            {
                var updateRequest = new UpdateItemRequest
                {
                    TableName = TableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "PK", new AttributeValue { S = task.PK } },
                        { "SK", new AttributeValue { S = task.SK } }
                    },
                    UpdateExpression = "SET #s = :newStatus, UpdatedAtUtc = :updatedAt, Version = :newVersion",
                    ConditionExpression = "Version = :expectedVersion",
                    ExpressionAttributeNames = new Dictionary<string, string> { { "#s", "Status" } },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":newStatus",       new AttributeValue { S = newStatus } },
                        { ":updatedAt",       new AttributeValue { S = task.UpdatedAtUtc.ToString("o") } },
                        { ":newVersion",      new AttributeValue { N = (task.Version + 1).ToString() } },
                        { ":expectedVersion", new AttributeValue { N = task.Version.ToString() } }
                    }
                };

                await _dynamoDb.UpdateItemAsync(updateRequest, cancellationToken);
                task.Version++;
                _logger.LogInformation("✅ [TaskStore] Durum güncellendi: {TaskId} | {Old} → {New}", task.TaskId, oldStatus, newStatus);
                return;
            }
            catch (ConditionalCheckFailedException ex)
            {
                task.Status = oldStatus;
                _logger.LogError(ex, "Yarış Durumu: Task versiyonu uyuşmadı!");
                throw new InvalidOperationException("Görev başka bir işlem tarafından değiştirilmiş. Lütfen tekrar deneyin.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ DynamoDB bağlantı hatası. In-Memory fallback aktif.");
                lock (Lock)
                {
                    _useFallback = true;
                }
            }
        }

        lock (Lock)
        {
            var key = $"{task.PK}#{task.SK}";
            if (İnMemoryFallback.TryGetValue(key, out var existing))
            {
                if (existing.Version != task.Version)
                    throw new InvalidOperationException($"Race Condition: Beklenen versiyon {task.Version}, bulunan {existing.Version}");
                task.Version++;
                İnMemoryFallback[key] = (task, task.Version);
            }
        }
        await Task.CompletedTask;
    }

    private static WorkflowTask MapFromAttributes(Dictionary<string, AttributeValue> attributes)
    {
        var task = new WorkflowTask
        {
            PK = attributes["PK"].S,
            SK = attributes["SK"].S,
            TenantId = Guid.Parse(attributes["TenantId"].S),
            TaskId = Guid.Parse(attributes["TaskId"].S),
            WorkflowDefinitionId = Guid.Parse(attributes["WorkflowDefinitionId"].S),
            WorkflowName = attributes["WorkflowName"].S,
            AssetId = Guid.Parse(attributes["AssetId"].S),
            AssetName = attributes["AssetName"].S,
            AssetType = attributes["AssetType"].S,
            Status = attributes["Status"].S,
            Description = attributes["Description"].S,
            SourceEventId = attributes["SourceEventId"].S,
            CreatedAtUtc = DateTime.Parse(attributes["CreatedAtUtc"].S),
            UpdatedAtUtc = DateTime.Parse(attributes["UpdatedAtUtc"].S),
            Version = int.Parse(attributes["Version"].S)
        };

        if (attributes.TryGetValue("AssignedUserId", out var assignedUserAttr))
        {
            task.AssignedUserId = Guid.Parse(assignedUserAttr.S);
        }

        if (attributes.TryGetValue("ExecutionContextJson", out var execCtxAttr))
        {
            task.ExecutionContextJson = execCtxAttr.S;
        }

        return task;
    }
}
