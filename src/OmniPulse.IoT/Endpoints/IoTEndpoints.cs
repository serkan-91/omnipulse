using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using OmniPulse.IoT.Features.Telemetry.IngestTelemetry;
using OmniPulse.IoT.Features.Telemetry.GetTelemetry;
using OmniPulse.IoT.Features.Telemetry.ReportDeviceStatus;
using OmniPulse.IoT.Features.DeviceCategories.GetCategoryTree;
using OmniPulse.IoT.Features.DeviceCategories.CreateCategory;
using OmniPulse.IoT.Features.DeviceCategories.UpdateCategory;
using OmniPulse.IoT.Features.DeviceCategories.DeleteCategory;
using OmniPulse.IoT.Features.Assets.CreateAsset;
using OmniPulse.IoT.Features.Assets.AssignResponsibleUser;
using OmniPulse.IoT.Features.Assets.GetAssets;
using OmniPulse.IoT.Features.Assets.MountDevice;
using OmniPulse.IoT.Features.Alarms.CreateAlarmRule;
using OmniPulse.IoT.Features.Alarms.GetAlarmRules;
using OmniPulse.IoT.Features.Telemetry.GetTelemetryReport;
using OmniPulse.IoT.Features.Devices.CreateDevice;
using OmniPulse.IoT.Features.Alarms.GetActiveAlarms;
using OmniPulse.IoT.Features.Devices.GetTestBenchDevices;
using OmniPulse.IoT.Features.Devices.SeedDemoData;
using OmniPulse.IoT.Features.Devices.CleanupDemoData;
using OmniPulse.IoT.Features.Devices.GetTenantDevices;

namespace OmniPulse.IoT;

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
