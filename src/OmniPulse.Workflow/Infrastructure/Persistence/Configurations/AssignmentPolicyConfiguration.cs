using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Workflow.Domain.Entities;

namespace OmniPulse.Workflow.Infrastructure.Persistence.Configurations;

public class AssignmentPolicyConfiguration : IEntityTypeConfiguration<AssignmentPolicy>
{
    public void Configure(EntityTypeBuilder<AssignmentPolicy> builder)
    {
        builder.ToTable("AssignmentPolicies");

        builder.HasKey(ap => ap.Id);

        builder.Property(ap => ap.RulesetJson)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasOne(ap => ap.WorkflowDefinition)
            .WithMany()
            .HasForeignKey(ap => ap.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.RoleAliasMapJson)
            .HasColumnName("RoleAliasMapJson")
            .HasColumnType("jsonb")
            .IsRequired(false);

        // ITenantEntity filter is configured dynamically in DbContext
    }
}
