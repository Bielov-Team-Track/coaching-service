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
        builder.Property(f => f.Comment).HasMaxLength(4000);

        // EventId is a cross-service reference - no FK constraint
        builder.HasMany(f => f.ImprovementPoints)
            .WithOne(ip => ip.Feedback)
            .HasForeignKey(ip => ip.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Praise)
            .WithOne(p => p.Feedback)
            .HasForeignKey<Praise>(p => p.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.RecipientUserId);
        builder.HasIndex(f => f.CoachUserId);
        builder.HasIndex(f => f.EventId);
    }
}
