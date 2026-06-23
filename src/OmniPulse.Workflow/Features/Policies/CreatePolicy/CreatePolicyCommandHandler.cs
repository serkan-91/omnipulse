using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Workflow.Domain.Entities;
using OmniPulse.Workflow.Infrastructure.Persistence;

namespace OmniPulse.Workflow.Features.Policies.CreatePolicy;

public class CreatePolicyCommandHandler(
    WorkflowDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<CreatePolicyCommand, CreatePolicyResponse>
{
    public async Task<CreatePolicyResponse> Handle(CreatePolicyCommand request, CancellationToken cancellationToken)
    {
        // 1. Tenant/Kiracı bağlamını alalım
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;

        // 2. Workflow tanımı var mı kontrol edelim
        var definitionExists = await dbContext.WorkflowDefinitions
            .AnyAsync(w => w.Id == request.WorkflowDefinitionId && !w.IsDeleted, cancellationToken);

        if (!definitionExists)
        {
            throw new KeyNotFoundException($"İlişkilendirilecek iş akışı tanımı [{request.WorkflowDefinitionId}] bulunamadı!");
        }

        // 3. Mevcut bir politika varsa üzerine yazalım/güncelleyelim, yoksa yeni oluşturalım
        var existingPolicy = await dbContext.AssignmentPolicies
            .FirstOrDefaultAsync(ap => ap.WorkflowDefinitionId == request.WorkflowDefinitionId, cancellationToken);

        AssignmentPolicy policy;

        if (existingPolicy != null)
        {
            existingPolicy.UpdateRuleset(request.RulesetJson);
            policy = existingPolicy;
            dbContext.AssignmentPolicies.Update(policy);
        }
        else
        {
            policy = AssignmentPolicy.Create(
                tenantId,
                request.WorkflowDefinitionId,
                request.RulesetJson
            );
            dbContext.AssignmentPolicies.Add(policy);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreatePolicyResponse(
            Id: policy.Id,
            TenantId: policy.TenantId,
            WorkflowDefinitionId: policy.WorkflowDefinitionId,
            RulesetJson: policy.RulesetJson
        );
    }
}
