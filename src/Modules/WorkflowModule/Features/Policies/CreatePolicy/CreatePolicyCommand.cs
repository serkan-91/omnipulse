using MediatR;

namespace OmniPulse.Modules.WorkflowModule.Features.Policies.CreatePolicy;

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
