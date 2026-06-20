using OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;
using OmniPulse.Modules.IoTModule.Features.Telemetry.GetTelemetry;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.GetCategoryTree;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.CreateCategory;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.UpdateCategory;
using OmniPulse.Modules.IoTModule.Features.DeviceCategories.DeleteCategory;
using OmniPulse.Modules.IoTModule.Features.Vehicles.CreateVehicle;
using OmniPulse.Modules.IoTModule.Features.Vehicles.AssignDriver;
using OmniPulse.Modules.IoTModule.Features.Vehicles.MountDevice;
using OmniPulse.Modules.IoTModule.Features.Vehicles.GetVehicles;
using OmniPulse.Modules.IoTModule.Features.Alarms.CreateAlarmRule;
using OmniPulse.Modules.IoTModule.Features.Alarms.GetAlarmRules;
using OmniPulse.Modules.IoTModule.Features.Telemetry.GetTelemetryReport;

namespace OmniPulse.Identity.API.Endpoints;

public static class IoTEndpoints
{
    public static IEndpointRouteBuilder MapIoTEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapIngestTelemetryEndpoint();
        app.MapGetTelemetryEndpoint();
        
        // Cihaz Kategorileri (Device Categories) Ağaç ve CRUD 🌳
        app.MapGetCategoryTreeEndpoint();
        app.MapCreateCategoryEndpoint();
        app.MapUpdateCategoryEndpoint();
        app.MapDeleteCategoryEndpoint();

        // Araç ve Donanım Yönetimi (Vehicles & Devices CRUD / Mounting) 🚛🔌
        app.MapCreateVehicleEndpoint();
        app.MapAssignDriverEndpoint();
        app.MapMountDeviceEndpoint();
        app.MapGetVehiclesEndpoint();

        // Alarm ve Kural Yönetimi (Alarm Rules) 🚨
        app.MapCreateAlarmRuleEndpoint();
        app.MapGetAlarmRulesEndpoint();

        // Raporlama ve Analitik (Reports) 📊
        app.MapGetTelemetryReportEndpoint();
        
        return app;
    }
}
