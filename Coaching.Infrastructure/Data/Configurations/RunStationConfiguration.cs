using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class RunStationConfiguration : IEntityTypeConfiguration<RunStation>
{
    public void Configure(EntityTypeBuilder<RunStation> builder)
    {
        builder.ToTable("RunStations");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(PlanStation.NameMaxLength)
            .IsRequired();

        builder.HasIndex(s => new { s.RunItemId, s.Order });

        builder.HasOne(s => s.RunItem)
            .WithMany(i => i.Stations)
            .HasForeignKey(s => s.RunItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RunStationItemConfiguration : IEntityTypeConfiguration<RunStationItem>
{
    public void Configure(EntityTypeBuilder<RunStationItem> builder)
    {
        builder.ToTable("RunStationItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Kind)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.Notes).HasMaxLength(PlanItem.NotesMaxLength);
        builder.Property(i => i.Title).HasMaxLength(PlanItem.TitleMaxLength);

        builder.HasIndex(i => new { i.RunStationId, i.Order });

        builder.HasOne(i => i.Station)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.RunStationId)
            .OnDelete(DeleteBehavior.Cascade);

        // No FK to Drills, exactly as TrainingPlanRunItem.DrillId has none: the point of a run
        // snapshot is to outlive the thing it copied, and a foreign key would stop the drill
        // being deleted — or take the finished run with it.
    }
}
