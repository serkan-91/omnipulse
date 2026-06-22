using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Devices.SeedDemoData;

public record SeedDemoDataCommand : IRequest<SeedDemoDataResponse>;

public record SeedDemoDataResponse(
    bool IsSuccess,
    string Message,
    int SectorsCount,
    int AssetsCount,
    int DevicesCount
);
