using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniPulse.Modules.WorkflowModule.Domain.Entities;
using OmniPulse.Modules.WorkflowModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.WorkflowModule.Features.Definitions.CreateDefinition;

public class CreateDefinitionCommandHandler(WorkflowDbContext dbContext)
    : IRequestHandler<CreateDefinitionCommand, CreateDefinitionResponse>
{
    public async Task<CreateDefinitionResponse> Handle(CreateDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = WorkflowDefinition.Create(
            request.Name,
            request.Description,
            request.TriggerCondition,
            request.DefaultTaskDescription
        );

        dbContext.WorkflowDefinitions.Add(definition);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateDefinitionResponse(
            Id: definition.Id,
            Name: definition.Name,
            Description: definition.Description,
            TriggerCondition: definition.TriggerCondition,
            DefaultTaskDescription: definition.DefaultTaskDescription
        );
    }
}
