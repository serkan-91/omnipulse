using MediatR;

namespace OmniPulse.Modules.WorkflowModule.Features.Workflows.ProcessTelemetryEvent;

public record ProcessTelemetryEventCommand(
    Guid TenantId,
    Guid DeviceId,
    string TelemetryKey,
    double TelemetryValue,
    string SourceEventId
) : IRequest<bool>;
