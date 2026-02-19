using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlayerMetricScoreConfiguration : IEntityTypeConfiguration<PlayerMetricScore>
{
    public void Configure(EntityTypeBuilder<PlayerMetricScore> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RawValue)
            .HasPrecision(10, 2);

        builder.Property(s => s.NormalizedScore)
            .HasPrecision(5, 4);

        builder.HasOne(s => s.Metric)
            .WithMany(m => m.PlayerScores)
            .HasForeignKey(s => s.MetricId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.EvaluationId, s.MetricId })
            .IsUnique();
    }
}
