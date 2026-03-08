using Coaching.Domain.Models.Drills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class DrillBookmarkConfiguration : IEntityTypeConfiguration<DrillBookmark>
{
    public void Configure(EntityTypeBuilder<DrillBookmark> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DrillId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        // Navigation properties
        builder.HasOne(e => e.Drill)
            .WithMany(d => d.Bookmarks)
            .HasForeignKey(e => e.DrillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one bookmark per user per drill
        builder.HasIndex(e => new { e.DrillId, e.UserId })
            .IsUnique();

        // Indexes for efficient queries
        builder.HasIndex(e => e.DrillId);
        builder.HasIndex(e => e.UserId);
    }
}
