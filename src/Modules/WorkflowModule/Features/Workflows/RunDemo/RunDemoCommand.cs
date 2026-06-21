using MediatR;

namespace OmniPulse.Modules.WorkflowModule.Features.Workflows.RunDemo;

public record RunDemoCommand : IRequest<RunDemoResult>;

public record RunDemoResult(
    string Summary,
    object SeedDetails,
    object ExecutionLogs
);
