using MediatR;

namespace OmniPulse.IoT.Features.Devices.CleanupDemoData;

public record CleanupDemoDataCommand : IRequest<CleanupDemoDataResponse>;

public record CleanupDemoDataResponse(
    bool IsSuccess,
    string Message
);
