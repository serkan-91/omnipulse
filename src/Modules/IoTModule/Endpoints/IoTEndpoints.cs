using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;
using OmniPulse.Modules.IoTModule.Features.Telemetry.GetTelemetry;
using OmniPulse.Modules.IoTModule.Features.Telemetry.ReportDeviceStatus;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.GetCategoryTree;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.CreateCategory;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.UpdateCategory;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.DeleteCategory;
using OmniPulse.Modules.IoTModule.Features.Assets.CreateAsset;
using OmniPulse.Modules.IoTModule.Features.Assets.AssignResponsibleUser;
using OmniPulse.Modules.IoTModule.Features.Assets.GetAssets;
using OmniPulse.Modules.IoTModule.Features.Assets.MountDevice;
using OmniPulse.Modules.IoTModule.Features.Alarms.CreateAlarmRule;
using OmniPulse.Modules.IoTModule.Features.Alarms.GetAlarmRules;
using OmniPulse.Modules.IoTModule.Features.Telemetry.GetTelemetryReport;
using OmniPulse.Modules.IoTModule.Features.Devices.CreateDevice;
using OmniPulse.Modules.IoTModule.Features.Alarms.GetActiveAlarms;
using OmniPulse.Modules.IoTModule.Features.Devices.GetTestBenchDevices;
using OmniPulse.Modules.IoTModule.Features.Devices.SeedDemoData;
using OmniPulse.Modules.IoTModule.Features.Devices.CleanupDemoData;
using OmniPulse.Modules.IoTModule.Features.Devices.GetTenantDevices;

namespace OmniPulse.Modules.IoTModule;

public static class IoTEndpoints
{
    public static IEndpointRouteBuilder MapIoTEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapIngestTelemetryEndpoint();
        app.MapGetTelemetryEndpoint();
        app.MapReportDeviceStatusEndpoint();
        
        // Cihazlar (Devices) Yönetimi 🔌
        app.MapCreateDeviceEndpoint();
        app.MapGetTestBenchDevicesEndpoint();
        app.MapGetTenantDevicesEndpoint();
        app.MapSeedDemoDataEndpoint();
        app.MapCleanupDemoDataEndpoint();
        
        // Cihaz Kategorileri (Device Categories) Ağaç ve CRUD 🌳
        app.MapGetCategoryTreeEndpoint();
        app.MapCreateCategoryEndpoint();
        app.MapUpdateCategoryEndpoint();
        app.MapDeleteCategoryEndpoint();

        // Varlık Yönetimi (Assets - Evrensel Model) 🏭🚛🌆
        app.MapCreateAssetEndpoint();
        app.MapAssignResponsibleUserEndpoint();
        app.MapMountDeviceToAssetEndpoint();
        app.MapGetAssetsEndpoint();

        // Alarm ve Kural Yönetimi (Alarm Rules) 🚨
        app.MapCreateAlarmRuleEndpoint();
        app.MapGetAlarmRulesEndpoint();
        app.MapGetActiveAlarmsEndpoint();

        // Raporlama ve Analitik (Reports) 📊
        app.MapGetTelemetryReportEndpoint();
        
        return app;
    }
}
