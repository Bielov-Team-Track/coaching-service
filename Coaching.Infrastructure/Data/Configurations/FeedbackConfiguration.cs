using Coaching.Domain.Models.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.RecipientUserId).IsRequired();
        builder.Property(f => f.CoachUserId).IsRequired();
        builder.Property(f => f.Content).HasColumnName("Content").HasMaxLength(50000);
        builder.Property(f => f.ContentPlainText).HasMaxLength(4000);

        // Phase A: Keep Comment column mapped in EF for backward compat.
        builder.Property(f => f.Comment).HasColumnName("Comment").HasMaxLength(4000);

        // Cross-service references — no FK constraints
        builder.HasMany(f => f.ImprovementPoints)
            .WithOne(ip => ip.Feedback)
            .HasForeignKey(ip => ip.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Praise)
            .WithOne(p => p.Feedback)
            .HasForeignKey<Praise>(p => p.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        // EvaluationId FK with SetNull
        builder.HasOne(f => f.Evaluation)
            .WithMany()
            .HasForeignKey(f => f.EvaluationId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(f => f.RecipientUserId);
        builder.HasIndex(f => f.CoachUserId);
        builder.HasIndex(f => f.EventId);
        builder.HasIndex(f => f.ClubId);
    }
}
