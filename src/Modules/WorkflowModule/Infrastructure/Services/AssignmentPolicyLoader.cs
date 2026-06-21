using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.WorkflowModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.WorkflowModule.Infrastructure.Services;

// 'class' yerine daha hafif ve yetenekli bir 'record'
public record AssignmentRuleset
{
    public string AssigneeType { get; init; } = "StaticUser";
    public Guid? UserId { get; init; }
    public Guid? FallbackUserId { get; init; }
}

public interface IAssignmentPolicyLoader
{
    Task<AssignmentRuleset> LoadPolicyAsync(Guid tenantId, Guid workflowDefinitionId, CancellationToken cancellationToken = default);
}

public class AssignmentPolicyLoader(WorkflowDbContext dbContext) : IAssignmentPolicyLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Tekrarlanan fallback nesnesini tek bir yerde tanımladık!
    private static readonly AssignmentRuleset DefaultFallbackRuleset = new()
    {
        AssigneeType = "ResponsibleUser",
        FallbackUserId = null
    };

    public async Task<AssignmentRuleset> LoadPolicyAsync(Guid tenantId, Guid workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var policy = await dbContext.AssignmentPolicies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ap => ap.TenantId == tenantId && 
                                       ap.WorkflowDefinitionId == workflowDefinitionId && 
                                       !ap.IsDeleted, 
                                 cancellationToken);

        if (policy == null || string.IsNullOrWhiteSpace(policy.RulesetJson))
        {
            return DefaultFallbackRuleset;
        }

        try
        {
            return JsonSerializer.Deserialize<AssignmentRuleset>(policy.RulesetJson, JsonOptions) ?? DefaultFallbackRuleset;
        }
        catch
        {
            // TODO: Serkan-sama, buraya bir ILogger enjekte edip deserialization hatasını loglayabiliriz! 
            // Böylece DB'deki bozuk JSON'ları hemen tespit edebiliriz. 😉
            return DefaultFallbackRuleset;
        }
    }
}