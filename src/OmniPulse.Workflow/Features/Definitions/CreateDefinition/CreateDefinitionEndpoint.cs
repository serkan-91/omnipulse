using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Workflow.Features.Definitions.CreateDefinition;

public static class CreateDefinitionEndpoint
{
    public static IEndpointRouteBuilder MapCreateDefinitionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/workflows/definitions", async (CreateDefinitionDto dto, ISender mediator) =>
            {
                var command = new CreateDefinitionCommand(
                    Name: dto.Name,
                    Description: dto.Description,
                    TriggerCondition: dto.TriggerCondition,
                    DefaultTaskDescription: dto.DefaultTaskDescription
                );

                var response = await mediator.Send(command);
                return Results.Created($"api/workflows/definitions/{response.Id}", response);
            })
            .WithName("CreateWorkflowDefinition")
            .WithTags("Workflow Definitions")
            .RequireAuthorization();

        return app;
    }
}

public record CreateDefinitionDto(
    string Name,
    string Description,
    string TriggerCondition,
    string DefaultTaskDescription
);
