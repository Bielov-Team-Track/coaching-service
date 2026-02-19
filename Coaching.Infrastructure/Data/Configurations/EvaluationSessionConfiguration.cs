using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class EvaluationSessionConfiguration : IEntityTypeConfiguration<EvaluationSession>
{
    public void Configure(EntityTypeBuilder<EvaluationSession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(2000);

        builder.Property(s => s.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(s => s.ClubId);
        builder.HasIndex(s => s.CoachUserId);
        builder.HasIndex(s => s.EventId);

        builder.HasMany(s => s.Participants)
            .WithOne(p => p.Session)
            .HasForeignKey(p => p.EvaluationSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
