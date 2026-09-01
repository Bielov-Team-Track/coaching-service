using Coaching.Application.DTOs.Drills;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;
using Coaching.Domain.Models.Templates;
using FluentAssertions;
using MockQueryable;
using NSubstitute;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Exceptions;
using Shared.Testing.Base;

namespace Coaching.Tests.Unit.Services;

/// <summary>
/// The drill editor sends its whole dial list with every save, and the reconciler makes the
/// drill match it. What these pin down: identity travels by id (a rename keeps every plan's
/// answers), a born dial reaches the plans already using the drill, and a dropped dial takes
/// its values with it.
/// </summary>
[TestFixture]
[Category("Unit")]
public class DrillDialReconcilerTests : UnitTestBase
{
    private IRepository<DrillDial> _dialRepository = null!;
    private IPlanItemRepository _itemRepository = null!;
    private IRepository<PlanStationItem> _stationItemRepository = null!;
    private IRepository<PlanItemDialValue> _valueRepository = null!;
    private DrillDialReconciler _sut = null!;

    private readonly List<PlanItem> _spine = [];
    private readonly List<PlanStationItem> _grouped = [];
    private readonly List<PlanItemDialValue> _values = [];
    private readonly List<PlanItemDialValue> _addedValues = [];
    private readonly List<PlanItemDialValue> _deletedValues = [];
    private readonly List<DrillDial> _addedDials = [];
    private readonly List<DrillDial> _deletedDials = [];

    private Drill _drill = null!;
    private Guid _planId;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

        _spine.Clear();
        _grouped.Clear();
        _values.Clear();
        _addedValues.Clear();
        _deletedValues.Clear();
        _addedDials.Clear();
        _deletedDials.Clear();

        _planId = Guid.NewGuid();
        _drill = new Drill { Name = "Serve receive", CreatedByUserId = Guid.NewGuid() };

        _dialRepository = Substitute.For<IRepository<DrillDial>>();
        _itemRepository = Substitute.For<IPlanItemRepository>();
        _stationItemRepository = Substitute.For<IRepository<PlanStationItem>>();
        _valueRepository = Substitute.For<IRepository<PlanItemDialValue>>();

        _itemRepository.Query().Returns(_ => _spine.BuildMock());
        _stationItemRepository.Query().Returns(_ => _grouped.BuildMock());
        _valueRepository.Query().Returns(_ => _values.BuildMock());

        _valueRepository.When(r => r.Add(Arg.Any<PlanItemDialValue>())).Do(c => _addedValues.Add(c.Arg<PlanItemDialValue>()));
        _valueRepository.When(r => r.Delete(Arg.Any<PlanItemDialValue>())).Do(c => _deletedValues.Add(c.Arg<PlanItemDialValue>()));
        _dialRepository.When(r => r.Add(Arg.Any<DrillDial>())).Do(c => _addedDials.Add(c.Arg<DrillDial>()));
        _dialRepository.When(r => r.Delete(Arg.Any<DrillDial>())).Do(c => _deletedDials.Add(c.Arg<DrillDial>()));

        _sut = new DrillDialReconciler(_dialRepository, _itemRepository, _stationItemRepository, _valueRepository);
    }

    [Test]
    public async Task ABornDialReachesEveryUse_ExceptOnesAlreadyAnswering()
    {
        var settled = SpineUse();
        SpineUse();
        GroupUse();
        _values.Add(new PlanItemDialValue { PlanId = _planId, ItemId = settled.Id, DialName = "balls", Value = "7" });

        await _sut.ReconcileAsync(_drill, [Input(null, "balls", "5")]);

        _addedDials.Should().ContainSingle(d => d.Name == "balls" && d.DefaultValue == "5" && d.Order == 0);
        _addedValues.Should().HaveCount(2, "the settled use keeps its own answer");
        _addedValues.Should().OnlyContain(v => v.DialName == "balls" && v.Value == "5");
        _addedValues.Should().Contain(v => v.StationItemId != null, "a use inside a station group counts");
    }

    [Test]
    public async Task RenamingByIdKeepsEveryPlansAnswer()
    {
        var dial = ExistingDial("balls", "5");
        var use = SpineUse();
        var answer = new PlanItemDialValue { PlanId = _planId, ItemId = use.Id, DialName = "balls", Value = "9" };
        _values.Add(answer);

        await _sut.ReconcileAsync(_drill, [Input(dial.Id, "serves", "5")]);

        dial.Name.Should().Be("serves");
        answer.DialName.Should().Be("serves");
        _deletedValues.Should().BeEmpty();
        _addedDials.Should().BeEmpty();
    }

    [Test]
    public async Task ADroppedDialTakesItsValuesWithIt()
    {
        var keep = ExistingDial("balls", "5");
        var drop = ExistingDial("serves", "3");
        var use = SpineUse();
        _values.Add(new PlanItemDialValue { PlanId = _planId, ItemId = use.Id, DialName = "serves", Value = "8" });

        await _sut.ReconcileAsync(_drill, [Input(keep.Id, "balls", "5")]);

        _deletedDials.Should().ContainSingle(d => d.Name == "serves");
        _deletedValues.Should().ContainSingle(v => v.DialName == "serves");
        keep.Name.Should().Be("balls");
    }

    [Test]
    public async Task AnIdTheDrillNoLongerHasIsSimplyBornAgain()
    {
        await _sut.ReconcileAsync(_drill, [Input(Guid.NewGuid(), "balls", "5")]);

        _addedDials.Should().ContainSingle(d => d.Name == "balls");
    }

    [Test]
    public async Task OrderFollowsTheList()
    {
        var second = ExistingDial("serves", "3");
        second.Order = 0;

        await _sut.ReconcileAsync(_drill, [Input(null, "balls", "5"), Input(second.Id, "serves", "3")]);

        _addedDials.Single().Order.Should().Be(0);
        second.Order.Should().Be(1);
    }

    [Test]
    public async Task TheSameNameTwiceIsRefused()
    {
        var act = () => _sut.ReconcileAsync(_drill, [Input(null, "balls", "5"), Input(null, "balls", "3")]);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task ANameOutsideTheTokenGrammarIsRefused()
    {
        var act = () => _sut.ReconcileAsync(_drill, [Input(null, "Balls", "5")]);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task AToggleWithoutItsSentencesIsRefused()
    {
        var act = () => _sut.ReconcileAsync(_drill, [new DrillDialInputDto(null, "quiet", DialKind.Toggle, "true")]);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    private static DrillDialInputDto Input(Guid? id, string name, string defaultValue) =>
        new(id, name, DialKind.Number, defaultValue);

    private DrillDial ExistingDial(string name, string defaultValue)
    {
        var dial = new DrillDial { DrillId = _drill.Id, Name = name, Kind = DialKind.Number, DefaultValue = defaultValue, Order = _drill.Dials.Count };
        _drill.Dials.Add(dial);
        return dial;
    }

    private PlanItem SpineUse()
    {
        var item = new PlanItem { TemplateId = _planId, DrillId = _drill.Id };
        _spine.Add(item);
        return item;
    }

    private PlanStationItem GroupUse()
    {
        var row = new PlanStationItem
        {
            DrillId = _drill.Id,
            Station = new PlanStation { Name = "Setters", Item = new PlanItem { TemplateId = _planId } },
        };
        _grouped.Add(row);
        return row;
    }
}
