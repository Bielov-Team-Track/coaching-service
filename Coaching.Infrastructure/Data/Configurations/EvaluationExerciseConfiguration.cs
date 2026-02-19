using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class EvaluationExerciseConfiguration : IEntityTypeConfiguration<EvaluationExercise>
{
    public void Configure(EntityTypeBuilder<EvaluationExercise> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.ClubId);
        builder.HasIndex(e => e.CreatedByUserId);

        builder.HasMany(e => e.Metrics)
            .WithOne(m => m.Exercise)
            .HasForeignKey(m => m.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
