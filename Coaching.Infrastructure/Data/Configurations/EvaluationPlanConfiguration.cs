using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class EvaluationPlanConfiguration : IEntityTypeConfiguration<EvaluationPlan>
{
    public void Configure(EntityTypeBuilder<EvaluationPlan> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(200);

        builder.Property(p => p.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(p => p.ClubId);
        builder.HasIndex(p => p.CreatedByUserId);

        builder.HasMany(p => p.Items)
            .WithOne(i => i.Plan)
            .HasForeignKey(i => i.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Sessions)
            .WithOne(s => s.EvaluationPlan)
            .HasForeignKey(s => s.EvaluationPlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
