using System;
using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Workflow.Domain.Entities;

/// <summary>
/// Küresel (Global) "Best Practice" iş akışı şablonu! 📋
/// </summary>
public class WorkflowDefinition : BaseEntity, IAuditableEntity, ISoftDelete
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    
    // Tetiklenme kuralı ifadesi (Örn: "temperature > 50" veya "fuel < 10")
    public string TriggerCondition { get; private set; } = null!;
    
    // Tetiklendiğinde oluşacak varsayılan görev açıklaması
    public string DefaultTaskDescription { get; private set; } = null!;

    // IAuditableEntity
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDelete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    private WorkflowDefinition() { }

    public static WorkflowDefinition Create(
        string name,
        string description,
        string triggerCondition,
        string defaultTaskDescription)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("İş akışı tanım adı boş bırakılamaz!", nameof(name));

        if (string.IsNullOrWhiteSpace(triggerCondition))
            throw new ArgumentException("Tetiklenme kuralı (TriggerCondition) boş bırakılamaz!", nameof(triggerCondition));

        return new WorkflowDefinition
        {
            Name = name.Trim(),
            Description = description.Trim(),
            TriggerCondition = triggerCondition.Trim(),
            DefaultTaskDescription = defaultTaskDescription.Trim()
        };
    }

    public void Update(string name, string description, string triggerCondition, string defaultTaskDescription)
    {
        Name = name.Trim();
        Description = description.Trim();
        TriggerCondition = triggerCondition.Trim();
        DefaultTaskDescription = defaultTaskDescription.Trim();
    }
}
