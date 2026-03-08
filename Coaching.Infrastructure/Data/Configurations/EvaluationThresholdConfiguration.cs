using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class EvaluationThresholdConfiguration : IEntityTypeConfiguration<EvaluationThreshold>
{
    public void Configure(EntityTypeBuilder<EvaluationThreshold> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Skill)
            .HasConversion<int?>();

        builder.Property(t => t.MinScore)
            .HasPrecision(4, 2);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.HasIndex(t => t.ClubId);
        builder.HasIndex(t => new { t.ClubId, t.Skill })
            .IsUnique();
    }
}
