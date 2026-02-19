using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlayerSkillScoreConfiguration : IEntityTypeConfiguration<PlayerSkillScore>
{
    public void Configure(EntityTypeBuilder<PlayerSkillScore> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Skill)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.EarnedPoints)
            .HasPrecision(10, 2);

        builder.Property(s => s.MaxPoints)
            .HasPrecision(10, 2);

        builder.Property(s => s.Score)
            .HasPrecision(4, 2);

        builder.Property(s => s.Level)
            .HasMaxLength(100);

        builder.HasIndex(s => new { s.EvaluationId, s.Skill })
            .IsUnique();
    }
}
