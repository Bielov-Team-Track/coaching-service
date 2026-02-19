using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class EvaluationParticipantConfiguration : IEntityTypeConfiguration<EvaluationParticipant>
{
    public void Configure(EntityTypeBuilder<EvaluationParticipant> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Source)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(p => new { p.EvaluationSessionId, p.PlayerId })
            .IsUnique();

        builder.HasIndex(p => p.PlayerId);

        builder.HasOne(p => p.Evaluation)
            .WithOne(e => e.Participant)
            .HasForeignKey<PlayerEvaluation>(e => e.EvaluationParticipantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
