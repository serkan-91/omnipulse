using MediatR;

namespace OmniPulse.Modules.WorkflowModule.Features.Definitions.CreateDefinition;

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
