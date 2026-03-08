using Coaching.Domain.Models.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class ImprovementPointDrillConfiguration : IEntityTypeConfiguration<ImprovementPointDrill>
{
    public void Configure(EntityTypeBuilder<ImprovementPointDrill> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DrillId).IsRequired();

        // Local FK to Drill (both in coaching-service now)
        builder.HasOne(d => d.Drill)
            .WithMany()
            .HasForeignKey(d => d.DrillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.ImprovementPointId, d.DrillId }).IsUnique();
        builder.HasIndex(d => d.DrillId);
    }
}
