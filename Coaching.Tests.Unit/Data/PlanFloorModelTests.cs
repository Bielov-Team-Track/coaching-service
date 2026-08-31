using Coaching.Domain.Models.Templates;
using Coaching.Infrastructure.Data.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Coaching.Tests.Unit.Data;

/// <summary>
/// The shape the floor tables must have. Asserted against the model rather than a migration
/// because the migration is generated from it: a rule that is not in the configuration is not
/// in the database either, however carefully the service checks it in memory.
/// </summary>
[TestFixture]
[Category("Unit")]
public class PlanFloorModelTests
{
    private IModel _model = null!;

    [OneTimeSetUp]
    public void BuildModel()
    {
        var options = new DbContextOptionsBuilder<CoachingDbContext>()
            .UseNpgsql("Host=model-only;Database=model-only")
            .Options;

        using var context = new CoachingDbContext(options);

        // The design-time model, not the runtime one: check constraints and other things only a
        // migration needs are trimmed out of the read-optimized model.
        _model = context.GetService<IDesignTimeModel>().Model;
    }

    private IEntityType Booking => _model.FindEntityType(typeof(PlanCourtBooking))!;
    private IEntityType Placement => _model.FindEntityType(typeof(PlanItemPlacement))!;

    [Test]
    public void TheFloorTablesAreNamed()
    {
        Booking.GetTableName().Should().Be("PlanCourtBookings");
        Placement.GetTableName().Should().Be("PlanItemPlacements");
    }

    [Test]
    public void ACourtIsBookedOncePerPlan()
    {
        // A court belongs to one venue, so the court alone settles which booking is which.
        var index = Booking.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "PlanId", "CourtId" }));

        index.IsUnique.Should().BeTrue();
    }

    [Test]
    public void AnActivityHasOnePlacePerVenue()
    {
        // One filtered index per anchor kind: unfiltered, every station-item row would collide
        // with every other on a null ItemId.
        foreach (var anchor in new[] { "ItemId", "StationItemId" })
        {
            var index = Placement.GetIndexes()
                .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "PlanId", "VenueId", anchor }));

            index.IsUnique.Should().BeTrue($"one place per {anchor} per venue");
            index.GetFilter().Should().Be($"\"{anchor}\" IS NOT NULL");
        }
    }

    [Test]
    public void AVenuesFloorHasAnIndexOfItsOwn()
    {
        // Neither filtered index above can serve it: every row fails one of the two filters.
        Placement.GetIndexes()
            .Should().Contain(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "PlanId", "VenueId" }));
    }

    [Test]
    public void APlacementAnchorsToExactlyOneKindOfActivity()
    {
        Placement.GetCheckConstraints()
            .Should().ContainSingle()
            .Which.Sql.Should().Be("(\"ItemId\" IS NULL) <> (\"StationItemId\" IS NULL)");
    }

    [Test]
    public void TheFloorGoesWithThePlan()
    {
        // Nothing on the floor outlives the plan it belongs to.
        Booking.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(TrainingPlan))
            .DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        Placement.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(TrainingPlan))
            .DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Test]
    public void ACourtIsOursUnlessSaidOtherwise()
    {
        Booking.FindProperty(nameof(PlanCourtBooking.IsOurs))!
            .GetDefaultValue().Should().Be(true);
    }
}
