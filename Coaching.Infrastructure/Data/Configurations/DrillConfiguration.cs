using Coaching.Domain.Models.Drills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class DrillConfiguration : IEntityTypeConfiguration<Drill>
{
    public void Configure(EntityTypeBuilder<Drill> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .IsRequired(false);

        builder.Property(e => e.Category)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Intensity)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Visibility)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.VideoUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(e => e.CreatedByUserId)
            .IsRequired();

        builder.Property(e => e.LikeCount)
            .HasDefaultValue(0);

        // Animations stored as JSONB array in PostgreSQL
        builder.Property(e => e.Animations)
            .HasColumnType("jsonb")
            .IsRequired(false);

        // Navigation properties
        builder.HasOne(e => e.Creator)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Attachments)
            .WithOne(a => a.Drill)
            .HasForeignKey(a => a.DrillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Likes)
            .WithOne(l => l.Drill)
            .HasForeignKey(l => l.DrillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Bookmarks)
            .WithOne(b => b.Drill)
            .HasForeignKey(b => b.DrillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Comments)
            .WithOne(c => c.Drill)
            .HasForeignKey(c => c.DrillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Equipment)
            .WithOne(e => e.Drill)
            .HasForeignKey(e => e.DrillId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.CreatedByUserId);
        builder.HasIndex(e => e.ClubId);
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.Visibility);
        builder.HasIndex(e => e.LikeCount);
    }
}
