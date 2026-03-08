using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class EvaluationMetricConfiguration : IEntityTypeConfiguration<EvaluationMetric>
{
    public void Configure(EntityTypeBuilder<EvaluationMetric> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.MaxPoints)
            .HasPrecision(10, 2);

        builder.Property(m => m.Config)
            .HasMaxLength(1000);

        builder.HasIndex(m => new { m.ExerciseId, m.Order });

        builder.HasMany(m => m.SkillWeights)
            .WithOne(w => w.Metric)
            .HasForeignKey(w => w.MetricId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
