using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlayerEvaluationConfiguration : IEntityTypeConfiguration<PlayerEvaluation>
{
    public void Configure(EntityTypeBuilder<PlayerEvaluation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Outcome)
            .HasConversion<int>();

        builder.Property(e => e.CoachNotes)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.EvaluationParticipantId)
            .IsUnique();

        builder.HasIndex(e => e.PlayerId);
        builder.HasIndex(e => e.EvaluatedByUserId);

        builder.HasMany(e => e.MetricScores)
            .WithOne(s => s.Evaluation)
            .HasForeignKey(s => s.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.SkillScores)
            .WithOne(s => s.Evaluation)
            .HasForeignKey(s => s.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
