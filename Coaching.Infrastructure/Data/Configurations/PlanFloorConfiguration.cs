using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlanCourtBookingConfiguration : IEntityTypeConfiguration<PlanCourtBooking>
{
    public void Configure(EntityTypeBuilder<PlanCourtBooking> builder)
    {
        builder.ToTable("PlanCourtBookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Split)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.IsOurs)
            .HasDefaultValue(true);

        builder.Property(b => b.TakenBy)
            .HasMaxLength(PlanCourtBooking.TakenByMaxLength);

        // A court belongs to one venue, so the court alone settles which booking is which.
        builder.HasIndex(b => new { b.PlanId, b.CourtId }).IsUnique();

        // The floor is always read one venue at a time.
        builder.HasIndex(b => new { b.PlanId, b.VenueId });

        builder.HasOne(b => b.Plan)
            .WithMany(p => p.CourtBookings)
            .HasForeignKey(b => b.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PlanItemPlacementConfiguration : IEntityTypeConfiguration<PlanItemPlacement>
{
    public void Configure(EntityTypeBuilder<PlanItemPlacement> builder)
    {
        // Exactly one anchor, said by the database rather than trusted: readers key rows on
        // whichever of the two is set, and a row with neither belongs to no activity at all.
        builder.ToTable("PlanItemPlacements", t => t.HasCheckConstraint(
            "CK_PlanItemPlacements_OneAnchor",
            "(\"ItemId\" IS NULL) <> (\"StationItemId\" IS NULL)"));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ZoneId)
            .HasMaxLength(PlanItemPlacement.ZoneIdMaxLength);

        // Neither unique index below can serve a plain venue read: every row fails one of the
        // two filters, so the floor read needs an index of its own.
        builder.HasIndex(p => new { p.PlanId, p.VenueId });

        // One place per activity per venue. Filtered because exactly one anchor is ever set:
        // without the filter every station-item row would collide on a null ItemId.
        builder.HasIndex(p => new { p.PlanId, p.VenueId, p.ItemId })
            .IsUnique()
            .HasFilter("\"ItemId\" IS NOT NULL");

        builder.HasIndex(p => new { p.PlanId, p.VenueId, p.StationItemId })
            .IsUnique()
            .HasFilter("\"StationItemId\" IS NOT NULL");

        builder.HasOne(p => p.Plan)
            .WithMany(t => t.Placements)
            .HasForeignKey(p => p.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
