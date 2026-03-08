using Coaching.Domain.Enums;
using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlayerExerciseScoreConfiguration : IEntityTypeConfiguration<PlayerExerciseScore>
{
    public void Configure(EntityTypeBuilder<PlayerExerciseScore> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Session)
            .WithMany(s => s.ExerciseScores)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Exercise)
            .WithMany()
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(EvaluationScoreStatus.Pending);

        builder.HasIndex(e => new { e.SessionId, e.PlayerId, e.ExerciseId }).IsUnique();
    }
}
