using Coaching.Domain.Models.Drills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class DrillCommentConfiguration : IEntityTypeConfiguration<DrillComment>
{
    public void Configure(EntityTypeBuilder<DrillComment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.DrillId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.ParentCommentId)
            .IsRequired(false);

        // Navigation properties
        builder.HasOne(e => e.Drill)
            .WithMany(d => d.Comments)
            .HasForeignKey(e => e.DrillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referencing relationship for replies
        builder.HasOne(e => e.ParentComment)
            .WithMany(e => e.Replies)
            .HasForeignKey(e => e.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for efficient queries
        builder.HasIndex(e => e.DrillId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ParentCommentId);
        builder.HasIndex(e => e.CreatedAt);

        // Query filter for soft delete
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
