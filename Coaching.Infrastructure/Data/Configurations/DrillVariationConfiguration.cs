using Coaching.Domain.Models.Drills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class DrillVariationConfiguration : IEntityTypeConfiguration<DrillVariation>
{
    public void Configure(EntityTypeBuilder<DrillVariation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Note)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(e => e.Order)
            .HasDefaultValue(0);

        // Navigation properties
        builder.HasOne(e => e.SourceDrill)
            .WithMany(d => d.Variations)
            .HasForeignKey(e => e.SourceDrillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TargetDrill)
            .WithMany()
            .HasForeignKey(e => e.TargetDrillId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.SourceDrillId);
        builder.HasIndex(e => e.TargetDrillId);
        builder.HasIndex(e => new { e.SourceDrillId, e.Order });
    }
}
