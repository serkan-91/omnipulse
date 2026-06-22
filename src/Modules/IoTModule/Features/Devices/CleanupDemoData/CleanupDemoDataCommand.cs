using MediatR;

namespace OmniPulse.Modules.IoTModule.Features.Devices.CleanupDemoData;

public record CleanupDemoDataCommand : IRequest<CleanupDemoDataResponse>;

public record CleanupDemoDataResponse(
    bool IsSuccess,
    string Message
);
