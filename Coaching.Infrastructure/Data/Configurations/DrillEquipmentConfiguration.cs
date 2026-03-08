using Coaching.Domain.Models.Drills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class DrillEquipmentConfiguration : IEntityTypeConfiguration<DrillEquipment>
{
    public void Configure(EntityTypeBuilder<DrillEquipment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.IsOptional)
            .HasDefaultValue(false);

        builder.Property(e => e.Order)
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(e => e.DrillId);
        builder.HasIndex(e => e.IsOptional);
    }
}
