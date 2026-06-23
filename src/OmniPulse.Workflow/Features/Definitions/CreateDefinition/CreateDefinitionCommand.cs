using MediatR;

namespace OmniPulse.Workflow.Features.Definitions.CreateDefinition;

public record CreateDefinitionCommand(
    string Name,
    string Description,
    string TriggerCondition,
    string DefaultTaskDescription
) : IRequest<CreateDefinitionResponse>;

public record CreateDefinitionResponse(
    Guid Id,
    string Name,
    string Description,
    string TriggerCondition,
    string DefaultTaskDescription
);
