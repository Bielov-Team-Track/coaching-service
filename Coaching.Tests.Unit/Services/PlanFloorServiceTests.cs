using Coaching.Application.DTOs.Templates;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;
using FluentAssertions;
using MockQueryable;
using NSubstitute;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Exceptions;
using Shared.Testing.Base;

namespace Coaching.Tests.Unit.Services;

/// <summary>
/// The floor of an event's plan: which of the venue's courts are ours tonight, how each is
/// divided, and where every activity happens. A template has none. The floor is kept per
/// venue, so an event that moves and comes back finds the floor it had.
/// </summary>
[TestFixture]
[Category("Unit")]
public class PlanFloorServiceTests : UnitTestBase
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid PlanId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid VenueId = Guid.NewGuid();
    private static readonly Guid OtherVenueId = Guid.NewGuid();
    private static readonly Guid CourtOneId = Guid.NewGuid();
    private static readonly Guid CourtTwoId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid OtherItemId = Guid.NewGuid();
    private static readonly Guid StationItemId = Guid.NewGuid();
    private static readonly Guid ForeignItemId = Guid.NewGuid();

    private readonly List<TrainingPlan> _plans = [];
    private readonly List<PlanItem> _items = [];
    private readonly List<PlanStationItem> _stationItems = [];
    private readonly List<PlanCourtBooking> _bookings = [];
    private readonly List<PlanItemPlacement> _placements = [];
    private readonly List<PlanCourtBooking> _deletedBookings = [];
    private readonly List<PlanItemPlacement> _deletedPlacements = [];

    private IEventsGrpcClient _events = null!;
    private IRepository<PlanCourtBooking> _bookingRepository = null!;
    private IRepository<PlanItemPlacement> _placementRepository = null!;
    private PlanFloorService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _plans.Clear();
        _items.Clear();
        _stationItems.Clear();
        _bookings.Clear();
        _placements.Clear();
        _deletedBookings.Clear();
        _deletedPlacements.Clear();

        _plans.Add(new TrainingPlan
        {
            Id = PlanId,
            Name = "Tuesday",
            CreatedByUserId = OwnerId,
            PlanType = PlanType.Instance,
            EventId = EventId,
        });

        // Two plan rows, and one row inside a station group of the second: the two kinds of
        // activity a placement can anchor to.
        _items.Add(new PlanItem { Id = ItemId, TemplateId = PlanId, Order = 0, Duration = 20 });
        var stationsRow = new PlanItem { Id = OtherItemId, TemplateId = PlanId, Kind = ItemKind.Stations, Order = 1, Duration = 30 };
        _items.Add(stationsRow);

        var station = new PlanStation { Id = Guid.NewGuid(), Name = "Setters", PlanItemId = stationsRow.Id, Item = stationsRow };
        _stationItems.Add(new PlanStationItem { Id = StationItemId, StationId = station.Id, Station = station, Order = 0, Duration = 15 });

        var planRepository = Substitute.For<ITrainingPlanRepository>();
        planRepository.Query().Returns(_ => _plans.BuildMock());

        var itemRepository = Substitute.For<IPlanItemRepository>();
        itemRepository.Query().Returns(_ => _items.BuildMock());

        var stationItemRepository = Substitute.For<IRepository<PlanStationItem>>();
        stationItemRepository.Query().Returns(_ => _stationItems.BuildMock());

        _bookingRepository = Substitute.For<IRepository<PlanCourtBooking>>();
        _bookingRepository.Query().Returns(_ => _bookings.BuildMock());
        _bookingRepository.When(r => r.Add(Arg.Any<PlanCourtBooking>()))
            .Do(c => _bookings.Add(c.Arg<PlanCourtBooking>()));
        _bookingRepository.When(r => r.Delete(Arg.Any<PlanCourtBooking>()))
            .Do(c =>
            {
                var row = c.Arg<PlanCourtBooking>();
                _deletedBookings.Add(row);
                _bookings.Remove(row);
            });

        _placementRepository = Substitute.For<IRepository<PlanItemPlacement>>();
        _placementRepository.Query().Returns(_ => _placements.BuildMock());
        _placementRepository.When(r => r.Add(Arg.Any<PlanItemPlacement>()))
            .Do(c => _placements.Add(c.Arg<PlanItemPlacement>()));
        _placementRepository.When(r => r.Delete(Arg.Any<PlanItemPlacement>()))
            .Do(c =>
            {
                var row = c.Arg<PlanItemPlacement>();
                _deletedPlacements.Add(row);
                _placements.Remove(row);
            });

        _events = Substitute.For<IEventsGrpcClient>();

        _sut = new PlanFloorService(
            planRepository,
            itemRepository,
            stationItemRepository,
            _bookingRepository,
            _placementRepository,
            _events);
    }

    private static SaveCourtBookingDto Booking(Guid courtId, CourtSplit split = CourtSplit.Full) =>
        new(courtId, true, null, split);

    private static SavePlacementDto ItemAt(Guid itemId, Guid courtId, string? zoneId = null) =>
        new(courtId, zoneId, itemId, null);

    private static SavePlacementDto StationItemAt(Guid stationItemId, Guid courtId, string? zoneId = null) =>
        new(courtId, zoneId, null, stationItemId);

    private Task<PlanFloorDto> Save(List<SaveCourtBookingDto> bookings, List<SavePlacementDto> placements, Guid? userId = null) =>
        _sut.PutFloorAsync(PlanId, VenueId, new SavePlanFloorDto(bookings, placements), userId ?? OwnerId);

    // ---------- what has a floor ----------

    [Test]
    public async Task PutFloorAsync_WhenThePlanIsATemplate_Refuses()
    {
        // Arrange — a template is written before anyone knows which gym it will be run in
        _plans[0].PlanType = PlanType.Template;
        _plans[0].EventId = null;

        // Act
        var save = async () => await Save([Booking(CourtOneId)], []);

        // Assert
        (await save.Should().ThrowAsync<BadRequestException>()).WithMessage("A template has no floor");
    }

    // ---------- what may be placed ----------

    [Test]
    public async Task PutFloorAsync_WhenAnItemIsNotInThisPlan_Refuses()
    {
        // Act
        var save = async () => await Save([Booking(CourtOneId)], [ItemAt(ForeignItemId, CourtOneId)]);

        // Assert
        var thrown = await save.Should().ThrowAsync<ValidationException>();
        thrown.Which.FieldErrors.Should().ContainSingle()
            .Which.Field.Should().Be("placements[0].itemId");
    }

    [Test]
    public async Task PutFloorAsync_AcceptsBothKindsOfActivity()
    {
        // Act — a plan row and a row inside a station group, side by side
        var floor = await Save(
            [Booking(CourtOneId, CourtSplit.Halves)],
            [ItemAt(ItemId, CourtOneId, CourtZones.Left), StationItemAt(StationItemId, CourtOneId, CourtZones.Right)]);

        // Assert
        floor.Placements.Should().HaveCount(2);
        floor.Placements.Should().ContainSingle(p => p.ItemId == ItemId && p.StationItemId == null);
        floor.Placements.Should().ContainSingle(p => p.StationItemId == StationItemId && p.ItemId == null);
    }

    [Test]
    public async Task PutFloorAsync_WhenAPlacementNamesBothKindsOfActivity_Refuses()
    {
        // Act
        var save = async () => await Save(
            [Booking(CourtOneId)],
            [new SavePlacementDto(CourtOneId, null, ItemId, StationItemId)]);

        // Assert
        var thrown = await save.Should().ThrowAsync<ValidationException>();
        thrown.Which.FieldErrors.Should().ContainSingle().Which.Code.Should().Be("INVALID_ANCHOR");
    }

    [Test]
    public async Task PutFloorAsync_WhenAPlacementNamesNoActivity_Refuses()
    {
        // Act
        var save = async () => await Save(
            [Booking(CourtOneId)],
            [new SavePlacementDto(CourtOneId, null, null, null)]);

        // Assert
        var thrown = await save.Should().ThrowAsync<ValidationException>();
        thrown.Which.FieldErrors.Should().ContainSingle().Which.Code.Should().Be("INVALID_ANCHOR");
    }

    // ---------- where it may be placed ----------

    [Test]
    public async Task PutFloorAsync_WhenTheZoneIsNotOnACourtSplitThatWay_Refuses()
    {
        // Arrange — a quarter's key on a court that is only halved
        // Act
        var save = async () => await Save(
            [Booking(CourtOneId, CourtSplit.Halves)],
            [ItemAt(ItemId, CourtOneId, CourtZones.LeftNear)]);

        // Assert
        var thrown = await save.Should().ThrowAsync<ValidationException>();
        thrown.Which.FieldErrors.Should().ContainSingle()
            .Which.Field.Should().Be("placements[0].zoneId");
    }

    [Test]
    public async Task PutFloorAsync_TheWholeSurfaceIsAPlaceOnADividedCourt()
    {
        // Arrange — a court's whole surface stays a place after it is split; its halves sit inside it
        // Act
        var floor = await Save(
            [Booking(CourtOneId, CourtSplit.Quarters)],
            [ItemAt(ItemId, CourtOneId), StationItemAt(StationItemId, CourtOneId, CourtZones.RightFar)]);

        // Assert
        floor.Placements.Should().ContainSingle(p => p.ItemId == ItemId && p.ZoneId == null);
    }

    [Test]
    public async Task PutFloorAsync_WhenTheCourtIsNotOnTheFloor_Refuses()
    {
        // Act — a placement on a court this venue's floor does not claim
        var save = async () => await Save([Booking(CourtOneId)], [ItemAt(ItemId, CourtTwoId)]);

        // Assert
        var thrown = await save.Should().ThrowAsync<ValidationException>();
        thrown.Which.FieldErrors.Should().ContainSingle().Which.Code.Should().Be("NOT_BOOKED");
    }

    [Test]
    public async Task PutFloorAsync_WhenOneActivityIsPlacedTwice_Refuses()
    {
        // Act
        var save = async () => await Save(
            [Booking(CourtOneId, CourtSplit.Halves), Booking(CourtTwoId)],
            [ItemAt(ItemId, CourtOneId, CourtZones.Left), ItemAt(ItemId, CourtTwoId)]);

        // Assert
        var thrown = await save.Should().ThrowAsync<ValidationException>();
        thrown.Which.FieldErrors.Should().ContainSingle().Which.Code.Should().Be("DUPLICATE");
    }

    // ---------- the payload is the set ----------

    [Test]
    public async Task PutFloorAsync_ThePayloadIsTheWholeFloor()
    {
        // Arrange — a floor that already has two courts and two placements
        await Save(
            [Booking(CourtOneId), Booking(CourtTwoId)],
            [ItemAt(ItemId, CourtOneId), StationItemAt(StationItemId, CourtTwoId)]);

        // Act — one court, one placement
        var floor = await Save([Booking(CourtOneId)], [ItemAt(ItemId, CourtOneId)]);

        // Assert
        floor.Bookings.Should().ContainSingle().Which.CourtId.Should().Be(CourtOneId);
        floor.Placements.Should().ContainSingle().Which.ItemId.Should().Be(ItemId);
        _bookings.Should().ContainSingle();
        _placements.Should().ContainSingle();
        _deletedBookings.Should().ContainSingle().Which.CourtId.Should().Be(CourtTwoId);
        _deletedPlacements.Should().ContainSingle().Which.StationItemId.Should().Be(StationItemId);
    }

    [Test]
    public async Task PutFloorAsync_LeavesAnotherVenuesFloorAlone()
    {
        // Arrange — the same plan was held at another venue, and still is
        _bookings.Add(new PlanCourtBooking { PlanId = PlanId, VenueId = OtherVenueId, CourtId = CourtTwoId });
        _placements.Add(new PlanItemPlacement { PlanId = PlanId, VenueId = OtherVenueId, CourtId = CourtTwoId, ItemId = ItemId });

        // Act
        await Save([Booking(CourtOneId)], [ItemAt(ItemId, CourtOneId)]);

        // Assert
        _deletedBookings.Should().BeEmpty();
        _deletedPlacements.Should().BeEmpty();
        _bookings.Should().Contain(b => b.VenueId == OtherVenueId && b.CourtId == CourtTwoId);
        _placements.Should().Contain(p => p.VenueId == OtherVenueId && p.ItemId == ItemId);
    }

    [Test]
    public async Task PutFloorAsync_MovingAnActivityKeepsItsRow()
    {
        // Arrange
        await Save([Booking(CourtOneId), Booking(CourtTwoId)], [ItemAt(ItemId, CourtOneId)]);
        var placed = _placements.Single();

        // Act — the same activity, a different court
        await Save([Booking(CourtOneId), Booking(CourtTwoId)], [ItemAt(ItemId, CourtTwoId)]);

        // Assert — updated in place, so no delete races the insert on the unique index
        _placements.Should().ContainSingle().Which.Should().BeSameAs(placed);
        placed.CourtId.Should().Be(CourtTwoId);
        _deletedPlacements.Should().BeEmpty();
    }

    [Test]
    public async Task PutFloorAsync_SavesTheWholeFloorAtOnce()
    {
        // Act
        await Save([Booking(CourtOneId)], [ItemAt(ItemId, CourtOneId)]);

        // Assert — one commit, so a floor never lands half-written
        await _bookingRepository.Received(1).SaveChangesAsync();
        await _placementRepository.DidNotReceive().SaveChangesAsync();
    }

    // ---------- reading it back ----------

    [Test]
    public async Task GetFloorAsync_DropsPlacementsWhoseActivityIsGone_ButKeepsTheRows()
    {
        // Arrange — saving a plan deletes and recreates its rows, so an anchor can vanish
        _bookings.Add(new PlanCourtBooking { PlanId = PlanId, VenueId = VenueId, CourtId = CourtOneId });
        _placements.Add(new PlanItemPlacement { PlanId = PlanId, VenueId = VenueId, CourtId = CourtOneId, ItemId = ItemId });
        _placements.Add(new PlanItemPlacement { PlanId = PlanId, VenueId = VenueId, CourtId = CourtOneId, ItemId = ForeignItemId });

        // Act
        var floor = await _sut.GetFloorAsync(PlanId, VenueId, OwnerId);

        // Assert
        floor.Placements.Should().ContainSingle().Which.ItemId.Should().Be(ItemId);
        floor.StalePlacements.Should().Be(1);
        _placements.Should().HaveCount(2);
        _deletedPlacements.Should().BeEmpty();
    }

    [Test]
    public async Task GetFloorAsync_ReadsOnlyTheVenueAskedFor()
    {
        // Arrange
        _bookings.Add(new PlanCourtBooking { PlanId = PlanId, VenueId = VenueId, CourtId = CourtOneId });
        _bookings.Add(new PlanCourtBooking { PlanId = PlanId, VenueId = OtherVenueId, CourtId = CourtTwoId });

        // Act
        var floor = await _sut.GetFloorAsync(PlanId, VenueId, OwnerId);

        // Assert
        floor.VenueId.Should().Be(VenueId);
        floor.Bookings.Should().ContainSingle().Which.CourtId.Should().Be(CourtOneId);
    }

    [Test]
    public async Task GetFloorAsync_LetsSomeoneAtTheEventSeeWhereTheyAre()
    {
        // Arrange — a player is not the plan's author but is standing on its floor
        _events.IsEventParticipantAsync(EventId, OtherUserId).Returns((true, true));
        _bookings.Add(new PlanCourtBooking { PlanId = PlanId, VenueId = VenueId, CourtId = CourtOneId });

        // Act
        var floor = await _sut.GetFloorAsync(PlanId, VenueId, OtherUserId);

        // Assert
        floor.Bookings.Should().ContainSingle();
    }

    // ---------- who may change it ----------

    [Test]
    public async Task PutFloorAsync_LetsAnEventAdminSaveAPlanTheyDoNotOwn()
    {
        // Arrange
        _events.IsEventAdminAsync(EventId, OtherUserId).Returns(true);

        // Act
        var floor = await Save([Booking(CourtOneId)], [ItemAt(ItemId, CourtOneId)], OtherUserId);

        // Assert
        floor.Bookings.Should().ContainSingle();
    }

    [Test]
    public async Task PutFloorAsync_WhenTheUserIsNeitherOwnerNorEventAdmin_Refuses()
    {
        // Act
        var save = async () => await Save([Booking(CourtOneId)], [], OtherUserId);

        // Assert
        await save.Should().ThrowAsync<ForbiddenException>();
    }
}
