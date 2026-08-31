using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlanCoachConfiguration : IEntityTypeConfiguration<PlanCoach>
{
    public void Configure(EntityTypeBuilder<PlanCoach> builder)
    {
        builder.ToTable("PlanCoaches");
        builder.HasKey(c => c.Id);

        // Assignment replaces the set rather than adding to it, so the same coach can only
        // ever be on a plan once. Rows are removed outright, not soft-deleted, which is what
        // lets this be unique.
        builder.HasIndex(c => new { c.PlanId, c.UserId }).IsUnique();
        builder.HasIndex(c => c.UserId);

        builder.HasOne(c => c.Plan)
            .WithMany(p => p.Coaches)
            .HasForeignKey(c => c.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PlanStationCoachConfiguration : IEntityTypeConfiguration<PlanStationCoach>
{
    public void Configure(EntityTypeBuilder<PlanStationCoach> builder)
    {
        builder.ToTable("PlanStationCoaches");
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => new { c.StationId, c.UserId }).IsUnique();
        builder.HasIndex(c => c.UserId);

        builder.HasOne(c => c.Station)
            .WithMany(s => s.Coaches)
            .HasForeignKey(c => c.StationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
