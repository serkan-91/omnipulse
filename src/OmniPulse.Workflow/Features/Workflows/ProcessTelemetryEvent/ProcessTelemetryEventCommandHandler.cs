using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Workflow.Domain.Entities;
using OmniPulse.Workflow.Infrastructure.Persistence;
using OmniPulse.Workflow.Infrastructure.Services;

namespace OmniPulse.Workflow.Features.Workflows.ProcessTelemetryEvent;

public class ProcessTelemetryEventCommandHandler(
    WorkflowDbContext dbContext,
    IWorkflowTaskStore taskStore,
    IAssignmentPolicyLoader policyLoader,
    IIotAssetService assetService,
    INotificationService notificationService,
    ILogger<ProcessTelemetryEventCommandHandler> logger)
    : IRequestHandler<ProcessTelemetryEventCommand, bool>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<bool> Handle(ProcessTelemetryEventCommand request, CancellationToken cancellationToken)
    {
        // 1. Mükerrer Mesaj/Olay Kontrolü (Idempotency)
        var isAlreadyProcessed = await taskStore.HasEventBeenProcessedAsync(request.TenantId, request.SourceEventId, cancellationToken);
        if (isAlreadyProcessed)
        {
            logger.LogInformation("ℹ️ [WorkflowEngine] {SourceEventId} olay kimliği bu tenant için zaten işlenmiş. Atlanıyor...", request.SourceEventId);
            return false;
        }

        // 2. Cihaza bağlı Varlık (Asset) Detaylarını IoT Modülünden Çek (Isolate Query)
        var asset = await assetService.GetAssetByDeviceAsync(request.DeviceId, cancellationToken);
        if (asset == null)
        {
            logger.LogWarning("⚠️ [WorkflowEngine] Donanıma ({DeviceId}) bağlı aktif bir varlık bulunamadı. İş akışı tetiklenemez.", request.DeviceId);
            return false;
        }

        // 3. Küresel Tanımlı İş Akışlarını Çek
        var definitions = await dbContext.WorkflowDefinitions
            .Where(w => !w.IsDeleted)
            .ToListAsync(cancellationToken);

        bool anyTriggered = false;

        foreach (var definition in definitions)
        {
            // Tetiklenme kuralını ayrıştır
            TriggerRule? rule = null;
            try
            {
                rule = JsonSerializer.Deserialize<TriggerRule>(definition.TriggerCondition, JsonOptions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [WorkflowEngine] {DefinitionName} için TriggerCondition JSON ayrıştırılamadı!", definition.Name);
                continue;
            }

            if (rule == null || !string.Equals(rule.TelemetryKey, request.TelemetryKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Kuralı değerlendir: Telemetri Değeri + Varlık Metadata + Kural Seti
            var isTriggered = WorkflowRuleEvaluator.Evaluate(request.TelemetryValue, asset.MetadataJson, rule);
            if (!isTriggered)
            {
                continue;
            }

            // Aktif Kart Spam Koruması: aynı varlık+workflow için açık görev varsa yenisini açma
            var hasActiveTask = await taskStore.HasActiveTaskAsync(
                request.TenantId, asset.Id, definition.Id, cancellationToken);
            if (hasActiveTask)
            {
                logger.LogInformation(
                    "⏭️ [WorkflowEngine] '{DefName}' için '{AssetName}' üzerinde zaten aktif bir görev mevcut. Atlanıyor.",
                    definition.Name, asset.Name);
                continue;
            }

            logger.LogInformation("🔥 [WorkflowEngine] Kural Tetiklendi! Tanım: {DefName}, Varlık: {AssetName}, Telemetri: {Key}={Val}", 
                definition.Name, asset.Name, request.TelemetryKey, request.TelemetryValue);

            // 4. Tenant-Scoped Assignment Policy (Atama Politikası) Yükle
            var policy = await policyLoader.LoadPolicyAsync(request.TenantId, definition.Id, cancellationToken);

            // 5. Görev Atanacak Kullanıcıyı Çözümle
            Guid? assignedUserId = null;
            bool isSensor = string.Equals(asset.Type, "Sensor", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(policy.AssigneeType, "ResponsibleUser", StringComparison.OrdinalIgnoreCase))
            {
                // Önce varlığın kendi zimmetlisine bak
                assignedUserId = asset.ResponsibleUserId;

                // Eğer kendi zimmetlisi boşsa ve üst varlık varsa, yukarı doğru tırmanarak sorumlu bul
                var currentParentId = asset.ParentAssetId;
                while (!assignedUserId.HasValue && currentParentId.HasValue)
                {
                    var parentAsset = await assetService.GetAssetByIdAsync(currentParentId.Value, cancellationToken);
                    if (parentAsset == null) break;

                    assignedUserId = parentAsset.ResponsibleUserId;
                    currentParentId = parentAsset.ParentAssetId;
                }

                // Hala bulunamadıysa fallback kullanıcıya ata
                assignedUserId ??= policy.FallbackUserId;
            }
            else if (string.Equals(policy.AssigneeType, "Supervisor", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(policy.AssigneeType, "ParentResponsibleUser", StringComparison.OrdinalIgnoreCase))
            {
                // Supervisor durumunda:
                // 1. Eğer doğrudan varlık bir sensör ise, sensörün ebeveyni "Ana Varlık"tır (Bant veya Tır). 
                //    Sensörün ebeveynini atlayıp (çünkü o da operatördür/sorumludur) bir üst seviyedeki Supervisor'a (şef/ustabaşı) gitmeliyiz.
                // 2. Eğer doğrudan varlık zaten ana varlık ise, direkt onun ebeveynine (bir üst seviyeye) gideriz.
                Guid? startParentId = null;
                if (isSensor)
                {
                    if (asset.ParentAssetId.HasValue)
                    {
                        var mainAsset = await assetService.GetAssetByIdAsync(asset.ParentAssetId.Value, cancellationToken);
                        if (mainAsset != null)
                        {
                            startParentId = mainAsset.ParentAssetId;
                        }
                    }
                }
                else
                {
                    startParentId = asset.ParentAssetId;
                }

                var currentParentId = startParentId;
                while (!assignedUserId.HasValue && currentParentId.HasValue)
                {
                    var parentAsset = await assetService.GetAssetByIdAsync(currentParentId.Value, cancellationToken);
                    if (parentAsset == null) break;

                    assignedUserId = parentAsset.ResponsibleUserId;
                    currentParentId = parentAsset.ParentAssetId;
                }

                // Ebeveynlerde bulunamazsa fallback kullanıcıya ata
                assignedUserId ??= policy.FallbackUserId;
            }
            else if (string.Equals(policy.AssigneeType, "StaticUser", StringComparison.OrdinalIgnoreCase))
            {
                // Doğrudan tanımlı statik kullanıcıya ata
                assignedUserId = policy.UserId;
            }

            // 6. DynamoDB Task Nesnesini Oluştur
            var taskId = Guid.NewGuid();
            var task = new WorkflowTask
            {
                PK = $"TENANT#{request.TenantId}",
                SK = $"TASK#{taskId}",
                TenantId = request.TenantId,
                TaskId = taskId,
                WorkflowDefinitionId = definition.Id,
                WorkflowName = definition.Name,
                AssetId = asset.Id,
                AssetName = asset.Name,
                AssetType = asset.Type,
                Status = "Pending",
                Description = $"{definition.DefaultTaskDescription} (Varlık: {asset.Name})",
                AssignedUserId = assignedUserId,
                SourceEventId = request.SourceEventId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Version = 1,
                ExecutionContextJson = JsonSerializer.Serialize(new
                {
                    TelemetryKey = request.TelemetryKey,
                    TelemetryValue = request.TelemetryValue,
                    EvaluatedThreshold = rule.StaticThreshold ?? 0.0, // metadata durumunda dinamik
                    SensorName = asset.DeviceName,
                    SensorSerialNumber = asset.DeviceSerialNumber
                }, JsonOptions)
            };

            // 7. DynamoDB'ye kaydet
            await taskStore.SaveTaskAsync(task, cancellationToken);

            // 8. Atanan kullanıcıya bildirim gönder
            if (assignedUserId.HasValue)
            {
                await notificationService.NotifyTaskAssignedAsync(new TaskAssignedNotification
                {
                    AssignedUserId = assignedUserId.Value,
                    TaskId         = taskId,
                    AssetName      = asset.Name,
                    WorkflowName   = definition.Name,
                    Description    = task.Description,
                    TenantId       = request.TenantId,
                    CreatedAtUtc   = task.CreatedAtUtc
                }, cancellationToken);
            }

            anyTriggered = true;
        }

        return anyTriggered;
    }
}
