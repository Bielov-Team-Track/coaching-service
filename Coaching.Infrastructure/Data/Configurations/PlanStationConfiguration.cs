using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlanStationConfiguration : IEntityTypeConfiguration<PlanStation>
{
    public void Configure(EntityTypeBuilder<PlanStation> builder)
    {
        builder.ToTable("PlanStations");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(PlanStation.NameMaxLength)
            .IsRequired();

        builder.HasIndex(s => new { s.PlanItemId, s.Order });

        // A group has no meaning without the block it splits, so it goes with it.
        builder.HasOne(s => s.Item)
            .WithMany(i => i.Stations)
            .HasForeignKey(s => s.PlanItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PlanStationItemConfiguration : IEntityTypeConfiguration<PlanStationItem>
{
    public void Configure(EntityTypeBuilder<PlanStationItem> builder)
    {
        builder.ToTable("PlanStationItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Kind)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.Notes).HasMaxLength(PlanItem.NotesMaxLength);
        builder.Property(i => i.Title).HasMaxLength(PlanItem.TitleMaxLength);

        builder.HasIndex(i => new { i.StationId, i.Order });
        builder.HasIndex(i => i.DrillId);

        builder.HasOne(i => i.Station)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.StationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Only a Drill row points at a drill, so the FK is optional. Restrict still applies:
        // a drill in use by a plan cannot be deleted out from under it.
        builder.HasOne(i => i.Drill)
            .WithMany()
            .HasForeignKey(i => i.DrillId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
