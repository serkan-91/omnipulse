using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniPulse.Modules.WorkflowModule.Domain.Entities;
using OmniPulse.Modules.WorkflowModule.Features.Workflows.ProcessTelemetryEvent;
using OmniPulse.Modules.WorkflowModule.Infrastructure.Persistence;
using OmniPulse.Modules.WorkflowModule.Infrastructure.Services;

namespace OmniPulse.Modules.WorkflowModule.Features.Workflows.RunDemo;

public class RunDemoCommandHandler(
    WorkflowDbContext dbContext,
    IWorkflowTaskStore taskStore,
    IMediator mediator,
    ILogger<RunDemoCommandHandler> logger)
    : IRequestHandler<RunDemoCommand, RunDemoResult>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<RunDemoResult> Handle(RunDemoCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("🎬 [WorkflowDemo] Kullanıcı Senaryo Simülasyonu Başlatıldı...");

        // 1. Tenant Tanımları
        var tenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"); // Lojistik ve Akıllı Altyapı
        var tenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"); // Fabrika Operasyonları

        // 2. Kullanıcı (Role/Personel) Tanımları (Tamamen Tenant atamalarına göre çalışır)
        var ayse = Guid.Parse("11111111-1111-1111-1111-111111111111");       // Tenant B - Bant A1 Sorumlusu Ayşe Hanım
        var huseyin = Guid.Parse("22222222-2222-2222-2222-222222222222");   // Tenant A - Tırcı Hüseyin Bey
        var fatma = Guid.Parse("33333333-3333-3333-3333-333333333333");     // Tenant B - Bant B1 Sorumlusu Fatma Hanım
        var mehmetUsta = Guid.Parse("44444444-4444-4444-4444-444444444444"); // Tenant B - Bakım Sorumlusu Mehmet Usta (Bant Serisi A1, A2, A3)

        // 3. Küresel İş Akışı Kuralları (Definitions)
        var ruleWater = new TriggerRule { TelemetryKey = "water_level", Operator = "<", StaticThreshold = 20.0 };
        var ruleIceCream = new TriggerRule { TelemetryKey = "temperature", Operator = ">", StaticThreshold = 4.0 };
        var ruleOil = new TriggerRule { TelemetryKey = "oil_level", Operator = "<", StaticThreshold = 30.0 };
        var ruleBeltBroken = new TriggerRule { TelemetryKey = "belt_vibration", Operator = ">", StaticThreshold = 100.0 };

        var defWater = await GetOrCreateDefinitionAsync("Suyu kontrol et!", "Su seviyesi kritik sınırın altına düştü.", JsonSerializer.Serialize(ruleWater, JsonOptions), "Su seviyesini kontrol et ve depoyu doldur.");
        var defIceCream = await GetOrCreateDefinitionAsync("Tırdaki dondurmalar erimesin!", "Soğutmalı kasa sıcaklık kontrolü.", JsonSerializer.Serialize(ruleIceCream, JsonOptions), "Sıcaklık kritik eşiği aştı! Soğutma ünitesini aç veya kontrol et.");
        var defOil = await GetOrCreateDefinitionAsync("Yağ kontrolü!", "Konveyör bant yağ seviyesi kontrolü.", JsonSerializer.Serialize(ruleOil, JsonOptions), "Bant yağ seviyesi kritik sınırın altında, yağlama yapın.");
        var defBeltBroken = await GetOrCreateDefinitionAsync("Bant koptu (Tamir)!", "Bant titreşimi/kopması durum kartı.", JsonSerializer.Serialize(ruleBeltBroken, JsonOptions), "Bant titreşimi aşırı yüksek veya bant koptu! Lütfen acil müdahale edin.");

        // 4. Tenant Bazlı Özelleştirilmiş Atama Politikaları (Assignment Policies)
        // Tenant A: 
        // - "Dondurmalar erimesin" iş akışını ilgili aracın zimmetli sorumlusuna (Hüseyin'e) ata.
        await SetOrCreatePolicyAsync(tenantA, defIceCream.Id, "{\"AssigneeType\":\"ResponsibleUser\",\"FallbackUserId\":\"22222222-2222-2222-2222-222222222222\"}");

        // Tenant B:
        // - "Suyu kontrol et" iş akışını ilgili varlığın kendi zimmetlisine (Ayşe'ye) ata (Bant A1'deki Ayşe Hanım su seviyesine bakacak).
        // - "Yağ kontrolü" iş akışını ilgili varlığın kendi zimmetlisine (Fatma'ya) ata.
        // - "Bant koptu (Tamir)" iş akışını doğrudan ebeveyn/seri sorumlusuna (Ustabaşı Mehmet Usta'ya) yönlendir (Supervisor).
        await SetOrCreatePolicyAsync(tenantB, defWater.Id, "{\"AssigneeType\":\"ResponsibleUser\",\"FallbackUserId\":\"11111111-1111-1111-1111-111111111111\"}");
        await SetOrCreatePolicyAsync(tenantB, defOil.Id, "{\"AssigneeType\":\"ResponsibleUser\",\"FallbackUserId\":\"33333333-3333-3333-3333-333333333333\"}");
        await SetOrCreatePolicyAsync(tenantB, defBeltBroken.Id, "{\"AssigneeType\":\"Supervisor\",\"FallbackUserId\":\"44444444-4444-4444-4444-444444444444\"}");

        // 5. Varlık Hiyerarşisi (Asset Hierarchy) ve Sensör (Device) Seeding (Direct SQL)
        var assetTruck = Guid.Parse("a1111111-1111-1111-1111-111111111111");
        var assetBeltGroupA = Guid.Parse("a3333333-3333-3333-3333-333333333333"); // Mehmet Usta'ya zimmetli ebeveyn grup (A Serisi)
        var assetBeltA1 = Guid.Parse("a4444444-4444-4444-4444-444444444444");     // Ayşe Hanım'a zimmetli mikro bant A1
        var assetBeltB1 = Guid.Parse("a5555555-5555-5555-5555-555555555555");     // Fatma Hanım'a zimmetli mikro bant B1

        var deviceTruck = Guid.Parse("d1111111-1111-1111-1111-111111111111");
        var deviceWater = Guid.Parse("d4444444-4444-4444-4444-444444444444");
        var deviceVibration = Guid.Parse("d5555555-5555-5555-5555-555555555555");
        var deviceOil = Guid.Parse("d6666666-6666-6666-6666-666666666666");

        await dbContext.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ""Devices"" WHERE ""Id"" IN ('d1111111-1111-1111-1111-111111111111', 'd4444444-4444-4444-4444-444444444444', 'd5555555-5555-5555-5555-555555555555', 'd6666666-6666-6666-6666-666666666666');
            DELETE FROM ""Assets"" WHERE ""Id"" IN ('a1111111-1111-1111-1111-111111111111', 'a1111111-1111-1111-1111-111111111112', 'a3333333-3333-3333-3333-333333333333', 'a4444444-4444-4444-4444-444444444444', 'a5555555-5555-5555-5555-555555555555', 'a4444444-4444-4444-4444-444444444445', 'a4444444-4444-4444-4444-444444444446', 'a5555555-5555-5555-5555-555555555556');
        ");

        // Varlık Hiyerarşisi Ekle
        // AssetType: 'Vehicle', 'ConveyorBelt' (string)
        await dbContext.Database.ExecuteSqlAsync($@"
            INSERT INTO ""Assets"" (""Id"", ""TenantId"", ""Name"", ""Type"", ""ParentAssetId"", ""ResponsibleUserId"", ""MetadataJson"", ""CreatedAtUtc"", ""IsDeleted"") VALUES 
            ({assetTruck}, {tenantA}, 'AUPanda01', 'Vehicle', null, {huseyin}, '{{""Brand"":""Volvo FH16""}}', NOW(), false),
            ({assetBeltGroupA}, {tenantB}, 'Konveyör Bant Serisi A', 'ConveyorBelt', null, {mehmetUsta}, '{{""supervisor"":""Mehmet Usta""}}', NOW(), false),
            ({assetBeltA1}, {tenantB}, 'Bant A1', 'ConveyorBelt', {assetBeltGroupA}, {ayse}, '{{""model"":""A-Type""}}', NOW(), false),
            ({assetBeltB1}, {tenantB}, 'Bant B1', 'ConveyorBelt', null, {fatma}, '{{""model"":""B-Type""}}', NOW(), false);
        ");

        // Cihaz ve Sensörleri Ekle (Fiziksel sensörler doğrudan ana makineye/taşıta bağlanıyor)
        await dbContext.Database.ExecuteSqlAsync($@"
            INSERT INTO ""Devices"" (""Id"", ""TenantId"", ""Name"", ""SerialNumber"", ""IsActive"", ""AssetId"", ""CreatedAtUtc"", ""IsDeleted"") VALUES 
            ({deviceTruck}, {tenantA}, 'Sıcaklık Sensörü', 'SN-AUPANDA-TEMP', true, {assetTruck}, NOW(), false),
            ({deviceWater}, {tenantB}, 'Su Seviye Sensörü', 'SN-BTA1-WATER', true, {assetBeltA1}, NOW(), false),
            ({deviceVibration}, {tenantB}, 'Titreşim Sensörü', 'SN-BTA1-VIB', true, {assetBeltA1}, NOW(), false),
            ({deviceOil}, {tenantB}, 'Yağ Sensörü', 'SN-BTB1-OIL', true, {assetBeltB1}, NOW(), false);
        ");

        // 6. Telemetri Olay Tetiklemeleri (ProcessTelemetryEventCommand)
        var logs = new List<string>();

        // Senaryo 1: Tenant B - Ayşe'nin A1 bandındaki su deposundan telemetry 'water_level' = 15.0 geldi. (Eşik 20.0 idi -> Tetiklenecek)
        // Politika gereği Ayşe'ye atama yapılması gerekir (ResponsibleUser politikası en yakın veliyi bulur).
        var evtId1 = $"EVT-{Guid.NewGuid()}";
        logs.Add($"[OLAY 1] Tenant B - A1 Su Seviyesi telemetrisi geldi: 15.0. Olay ID: {evtId1}");
        var trig1 = await mediator.Send(new ProcessTelemetryEventCommand(tenantB, deviceWater, "water_level", 15.0, evtId1));
        logs.Add($"[OLAY 1 SONUÇ] Tetiklenme: {trig1}");

        // Senaryo 2: Tenant A - Hüseyin'in Tırından sıcaklık 6.5 derece geldi. (Eşik 4.0 idi -> Tetiklenecek)
        // Politika gereği Tır'ın zimmetli sorumlusu olan Hüseyin'e atanmalı.
        var evtId2 = $"EVT-{Guid.NewGuid()}";
        logs.Add($"[OLAY 2] Tenant A - Tır Sıcaklık telemetrisi geldi: 6.5°C. Olay ID: {evtId2}");
        var trig2 = await mediator.Send(new ProcessTelemetryEventCommand(tenantA, deviceTruck, "temperature", 6.5, evtId2));
        logs.Add($"[OLAY 2 SONUÇ] Tetiklenme: {trig2}");

        // Senaryo 3: Tenant B - Bant B1'den yağ seviyesi 22.0 geldi. (Eşik 30.0 idi -> Tetiklenecek)
        // Politika gereği "Yağ Kontrolü" iş akışı ResponsibleUser tipindedir -> Bant B1'in doğrudan sorumlusu olan Fatma Hanım'a atanmalı.
        var evtId3 = $"EVT-{Guid.NewGuid()}";
        logs.Add($"[OLAY 3] Tenant B - Bant B1 Yağ telemetrisi geldi: 22.0. Olay ID: {evtId3}");
        var trig3 = await mediator.Send(new ProcessTelemetryEventCommand(tenantB, deviceOil, "oil_level", 22.0, evtId3));
        logs.Add($"[OLAY 3 SONUÇ] Tetiklenme: {trig3}");

        // Senaryo 4: Tenant B - Bant A1'den titreşim 145.0 geldi. (Eşik 100.0 idi -> Tetiklenecek)
        // Bant A1'in doğrudan sorumlusu Ayşe Hanım olmasına rağmen, "Bant koptu (Tamir)" iş akışı politikasında AssigneeType = "Supervisor" seçilmiştir.
        // Bu yüzden motor Ayşe Hanım'ı atlayarak doğrudan üst varlığın sorumlusu olan Ustabaşı Mehmet Usta'ya görevi atar!
        var evtId4 = $"EVT-{Guid.NewGuid()}";
        logs.Add($"[OLAY 4] Tenant B - Bant A1 Titreşim telemetrisi geldi: 145.0 (Bant Koptu!). Olay ID: {evtId4}");
        var trig4 = await mediator.Send(new ProcessTelemetryEventCommand(tenantB, deviceVibration, "belt_vibration", 145.0, evtId4));
        logs.Add($"[OLAY 4 SONUÇ] Tetiklenme: {trig4}");

        // 7. DynamoDB Task Deposundan Oluşturulan Görevleri Çekip Listele
        var tasksTenantA = await taskStore.GetTasksByTenantAsync(tenantA);
        var tasksTenantB = await taskStore.GetTasksByTenantAsync(tenantB);

        var allTasks = tasksTenantA.Concat(tasksTenantB).ToList();

        var taskDetailsList = allTasks.Select(t => new
        {
            t.TaskId,
            t.TenantId,
            TenantName = t.TenantId == tenantA ? "Tenant A (Lojistik & Altyapı)" : "Tenant B (Fabrika)",
            t.WorkflowName,
            t.AssetName,
            t.AssetType,
            t.Status,
            t.Description,
            t.AssignedUserId,
            AssignedPerson = t.AssignedUserId == ayse ? "Ayşe Hanım (Bant A1 Sorumlusu)" :
                              t.AssignedUserId == huseyin ? "Hüseyin Bey (Tırcı)" :
                              t.AssignedUserId == fatma ? "Fatma Hanım (Bant B1 Sorumlusu)" :
                              t.AssignedUserId == mehmetUsta ? "Mehmet Usta (Bakım Sorumlusu / Seri Sorumlusu)" : "Bilinmeyen Personel",
            t.SourceEventId,
            t.Version,
            t.ExecutionContextJson
        }).ToList();

        return new RunDemoResult(
            Summary: "Kullanıcı Senaryoları Başarıyla Simüle Edildi! 🎭",
            SeedDetails: new
            {
                TenantAId = tenantA,
                TenantBId = tenantB,
                RoleSeeding = new Dictionary<string, string>
                {
                    { ayse.ToString(), "Tenant B - Ayşe Hanım" },
                    { huseyin.ToString(), "Tenant A - Hüseyin Bey" },
                    { fatma.ToString(), "Tenant B - Fatma Hanım" },
                    { mehmetUsta.ToString(), "Tenant B - Mehmet Usta (A1-A2-A3 Serisi)" }
                }
            },
            ExecutionLogs: new
            {
                SimulatedEvents = logs,
                DynamoDB_CreatedTasks = taskDetailsList
            }
        );
    }

    private async Task<WorkflowDefinition> GetOrCreateDefinitionAsync(
        string name,
        string description,
        string triggerCondition,
        string defaultTaskDescription)
    {
        var existing = await dbContext.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.Name == name && !w.IsDeleted);

        if (existing != null) return existing;

        var def = WorkflowDefinition.Create(name, description, triggerCondition, defaultTaskDescription);
        dbContext.WorkflowDefinitions.Add(def);
        await dbContext.SaveChangesAsync();
        return def;
    }

    private async Task SetOrCreatePolicyAsync(Guid tenantId, Guid workflowDefinitionId, string rulesetJson)
    {
        var existing = await dbContext.AssignmentPolicies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ap => ap.TenantId == tenantId && 
                                       ap.WorkflowDefinitionId == workflowDefinitionId && 
                                       !ap.IsDeleted);

        if (existing != null)
        {
            existing.UpdateRuleset(rulesetJson);
            dbContext.AssignmentPolicies.Update(existing);
        }
        else
        {
            var policy = AssignmentPolicy.Create(tenantId, workflowDefinitionId, rulesetJson);
            dbContext.AssignmentPolicies.Add(policy);
        }

        await dbContext.SaveChangesAsync();
    }
}
