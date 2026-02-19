using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class EvaluationPlanItemConfiguration : IEntityTypeConfiguration<EvaluationPlanItem>
{
    public void Configure(EntityTypeBuilder<EvaluationPlanItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.HasOne(i => i.Exercise)
            .WithMany(e => e.PlanItems)
            .HasForeignKey(i => i.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.PlanId, i.Order });
    }
}
