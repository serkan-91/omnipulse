using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OmniPulse.Workflow.Features.Policies.CreatePolicy;

public static class CreatePolicyEndpoint
{
    public static IEndpointRouteBuilder MapCreatePolicyEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/workflows/policies", async (CreatePolicyDto dto, ISender mediator) =>
            {
                var command = new CreatePolicyCommand(
                    WorkflowDefinitionId: dto.WorkflowDefinitionId,
                    RulesetJson: dto.RulesetJson
                );

                var response = await mediator.Send(command);
                return Results.Created($"api/workflows/policies/{response.Id}", response);
            })
            .WithName("CreateAssignmentPolicy")
            .WithTags("Workflow Policies")
            .RequireAuthorization();

        return app;
    }
}

public record CreatePolicyDto(
    Guid WorkflowDefinitionId,
    string RulesetJson
);
