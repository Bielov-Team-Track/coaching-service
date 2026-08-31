using Coaching.Domain.Models.Drills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class DrillDialConfiguration : IEntityTypeConfiguration<DrillDial>
{
    public void Configure(EntityTypeBuilder<DrillDial> builder)
    {
        builder.ToTable("DrillDials");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(DrillDial.NameMaxLength);

        builder.Property(d => d.Kind)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.DefaultValue)
            .IsRequired()
            .HasMaxLength(DrillDial.ValueMaxLength);

        builder.Property(d => d.OnText).HasMaxLength(DrillDial.ValueMaxLength);
        builder.Property(d => d.OffText).HasMaxLength(DrillDial.ValueMaxLength);
        builder.Property(d => d.OnLabel).HasMaxLength(DrillDial.LabelMaxLength);
        builder.Property(d => d.OffLabel).HasMaxLength(DrillDial.LabelMaxLength);

        builder.Property(d => d.Order).HasDefaultValue(0);

        // The name is the token in the instructions, so two dials cannot share one: the
        // splice would have no way to tell which value it meant.
        builder.HasIndex(d => new { d.DrillId, d.Name }).IsUnique();
        builder.HasIndex(d => new { d.DrillId, d.Order });

        // A dial is meaningless without the drill whose words it names.
        builder.HasOne(d => d.Drill)
            .WithMany(drill => drill.Dials)
            .HasForeignKey(d => d.DrillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
