using Coaching.Domain.Models.Drills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class DrillAttachmentConfiguration : IEntityTypeConfiguration<DrillAttachment>
{
    public void Configure(EntityTypeBuilder<DrillAttachment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.FileUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.FileType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.FileSize)
            .IsRequired();

        builder.Property(e => e.Order)
            .HasDefaultValue(0);

        builder.Property(e => e.DrillId)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.DrillId);
        builder.HasIndex(e => new { e.DrillId, e.Order });
    }
}
