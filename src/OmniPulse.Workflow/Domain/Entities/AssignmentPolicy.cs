using System;
using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Workflow.Domain.Entities;

/// <summary>
/// Tenant bazlı özelleştirilmiş görev atama politikası! ⚙️🔐
/// Hangi iş akışının kime/nereye atanacağını tanımlar.
/// </summary>
public class AssignmentPolicy : BaseEntity, IAuditableEntity, ITenantEntity, ISoftDelete
{
    public Guid TenantId { get; set; }
    
    public Guid WorkflowDefinitionId { get; private set; }
    public WorkflowDefinition WorkflowDefinition { get; private set; } = null!;

    // Atama kurallarını içeren JSON (Örn: {"AssigneeType": "ResponsibleUser", "FallbackUserId": "..."})
    public string RulesetJson { get; private set; } = null!;

    // IAuditableEntity
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDelete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    private AssignmentPolicy() { }

    public static AssignmentPolicy Create(
        Guid tenantId,
        Guid workflowDefinitionId,
        string rulesetJson)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Kiracı ID'si boş bırakılamaz!", nameof(tenantId));

        if (workflowDefinitionId == Guid.Empty)
            throw new ArgumentException("İş akışı tanım ID'si boş bırakılamaz!", nameof(workflowDefinitionId));

        if (string.IsNullOrWhiteSpace(rulesetJson))
            throw new ArgumentException("Kural seti (rulesetJson) boş bırakılamaz!", nameof(rulesetJson));

        return new AssignmentPolicy
        {
            TenantId = tenantId,
            WorkflowDefinitionId = workflowDefinitionId,
            RulesetJson = rulesetJson.Trim()
        };
    }

    public void UpdateRuleset(string rulesetJson)
    {
        if (string.IsNullOrWhiteSpace(rulesetJson))
            throw new ArgumentException("Kural seti boş bırakılamaz!", nameof(rulesetJson));

        RulesetJson = rulesetJson.Trim();
    }

    /// <summary>
    /// Tenant'a özel rol eşleştirme haritası (JSON).
    /// Örn: {"MAINTENANCE_ROLE": "44444444-...", "OPERATOR_ROLE": "11111111-..."}
    /// </summary>
    public string? RoleAliasMapJson { get; private set; }

    public void UpdateRoleAliasMap(string? roleAliasMapJson) => RoleAliasMapJson = roleAliasMapJson;
}
