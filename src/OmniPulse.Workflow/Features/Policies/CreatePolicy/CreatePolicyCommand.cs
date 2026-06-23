using MediatR;

namespace OmniPulse.Workflow.Features.Policies.CreatePolicy;

public record CreatePolicyCommand(
    Guid WorkflowDefinitionId,
    string RulesetJson
) : IRequest<CreatePolicyResponse>;

public record CreatePolicyResponse(
    Guid Id,
    Guid TenantId,
    Guid WorkflowDefinitionId,
    string RulesetJson
);
