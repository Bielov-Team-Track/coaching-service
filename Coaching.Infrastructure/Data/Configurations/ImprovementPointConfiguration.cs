using Coaching.Domain.Models.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class ImprovementPointConfiguration : IEntityTypeConfiguration<ImprovementPoint>
{
    public void Configure(EntityTypeBuilder<ImprovementPoint> builder)
    {
        builder.HasKey(ip => ip.Id);
        builder.Property(ip => ip.Description).IsRequired().HasMaxLength(1000);

        builder.HasMany(ip => ip.AttachedDrills)
            .WithOne(d => d.ImprovementPoint)
            .HasForeignKey(d => d.ImprovementPointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ip => ip.MediaLinks)
            .WithOne(m => m.ImprovementPoint)
            .HasForeignKey(m => m.ImprovementPointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ip => new { ip.FeedbackId, ip.Order });
    }
}
