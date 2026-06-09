using Coaching.Application.DTOs.Drills;
using Coaching.Application.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;
using FluentAssertions;

namespace Coaching.Tests.Unit.Services;

[TestFixture]
[Category("Unit")]
public class DrillQueryFilterTests
{
    private static readonly Guid Me = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();
    private static readonly Guid ClubA = Guid.NewGuid();
    private static readonly Guid ClubB = Guid.NewGuid();

    private static Drill Drill(
        string name = "Drill",
        Guid? createdBy = null,
        Guid? clubId = null,
        DrillVisibility visibility = DrillVisibility.Public,
        DrillCategory category = DrillCategory.Technical,
        DrillIntensity intensity = DrillIntensity.Medium,
        DrillSkill[]? skills = null,
        string[]? equipment = null,
        Guid[]? likedBy = null,
        Guid[]? bookmarkedBy = null,
        string? description = null)
    {
        return new Drill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedByUserId = createdBy ?? Other,
            ClubId = clubId,
            Visibility = visibility,
            Category = category,
            Intensity = intensity,
            Skills = skills ?? [],
            Equipment = (equipment ?? []).Select((e, i) => new DrillEquipment { Name = e, Order = i }).ToList(),
            Likes = (likedBy ?? []).Select(u => new DrillLike { UserId = u }).ToList(),
            Bookmarks = (bookmarkedBy ?? []).Select(u => new DrillBookmark { UserId = u }).ToList(),
        };
    }

    private static List<Drill> Run(IEnumerable<Drill> drills, Func<IQueryable<Drill>, IQueryable<Drill>> apply)
        => apply(drills.AsQueryable()).ToList();

    [Test]
    public void Scope_Mine_returns_only_drills_created_by_user()
    {
        var mine = Drill(createdBy: Me);
        var theirs = Drill(createdBy: Other);

        var result = Run([mine, theirs], q => q.ApplyScope(DrillScope.Mine, Me, ClubA));

        result.Should().ContainSingle().Which.Should().Be(mine);
    }

    [Test]
    public void Scope_Club_returns_only_contextual_club_drills()
    {
        var clubADrill = Drill(clubId: ClubA);
        var clubBDrill = Drill(clubId: ClubB);
        var noClub = Drill();

        var result = Run([clubADrill, clubBDrill, noClub], q => q.ApplyScope(DrillScope.Club, Me, ClubA));

        result.Should().ContainSingle().Which.Should().Be(clubADrill);
    }

    [Test]
    public void Scope_Club_without_club_context_returns_nothing()
    {
        var result = Run([Drill(clubId: ClubA), Drill()], q => q.ApplyScope(DrillScope.Club, Me, null));

        result.Should().BeEmpty();
    }

    [Test]
    public void Scope_Saved_returns_only_bookmarked_by_user()
    {
        var savedByMe = Drill(bookmarkedBy: [Me]);
        var savedByOther = Drill(bookmarkedBy: [Other]);

        var result = Run([savedByMe, savedByOther], q => q.ApplyScope(DrillScope.Saved, Me, null));

        result.Should().ContainSingle().Which.Should().Be(savedByMe);
    }

    [Test]
    public void Scope_Liked_returns_only_liked_by_user()
    {
        var likedByMe = Drill(likedBy: [Me]);
        var likedByOther = Drill(likedBy: [Other]);

        var result = Run([likedByMe, likedByOther], q => q.ApplyScope(DrillScope.Liked, Me, null));

        result.Should().ContainSingle().Which.Should().Be(likedByMe);
    }

    [Test]
    public void Scope_All_returns_public_own_and_contextual_club_only()
    {
        var publicDrill = Drill(createdBy: Other, visibility: DrillVisibility.Public);
        var myPrivate = Drill(createdBy: Me, visibility: DrillVisibility.Private);
        var contextClub = Drill(createdBy: Other, clubId: ClubA, visibility: DrillVisibility.Private);
        var otherClubPrivate = Drill(createdBy: Other, clubId: ClubB, visibility: DrillVisibility.Private);

        var result = Run([publicDrill, myPrivate, contextClub, otherClubPrivate],
            q => q.ApplyScope(DrillScope.All, Me, ClubA));

        result.Should().BeEquivalentTo([publicDrill, myPrivate, contextClub]);
        result.Should().NotContain(otherClubPrivate);
    }

    [Test]
    public void Categories_filter_is_OR_within_dimension()
    {
        var warmup = Drill(category: DrillCategory.Warmup);
        var game = Drill(category: DrillCategory.Game);
        var cooldown = Drill(category: DrillCategory.Cooldown);
        var filter = new DrillFilterRequest { Categories = [DrillCategory.Warmup, DrillCategory.Game] };

        var result = Run([warmup, game, cooldown], q => q.ApplyAttributeFilters(filter));

        result.Should().BeEquivalentTo([warmup, game]);
    }

    [Test]
    public void Skills_filter_matches_drills_having_any_selected_skill()
    {
        var serving = Drill(skills: [DrillSkill.Serving]);
        var passingSetting = Drill(skills: [DrillSkill.Passing, DrillSkill.Setting]);
        var blocking = Drill(skills: [DrillSkill.Blocking]);
        var filter = new DrillFilterRequest { Skills = [DrillSkill.Serving, DrillSkill.Setting] };

        var result = Run([serving, passingSetting, blocking], q => q.ApplyAttributeFilters(filter));

        result.Should().BeEquivalentTo([serving, passingSetting]);
    }

    [Test]
    public void Singular_skill_is_used_when_no_array_supplied()
    {
        var serving = Drill(skills: [DrillSkill.Serving]);
        var blocking = Drill(skills: [DrillSkill.Blocking]);
        var filter = new DrillFilterRequest { Skill = DrillSkill.Serving };

        var result = Run([serving, blocking], q => q.ApplyAttributeFilters(filter));

        result.Should().ContainSingle().Which.Should().Be(serving);
    }

    [Test]
    public void Intensities_filter_is_OR_within_dimension()
    {
        var low = Drill(intensity: DrillIntensity.Low);
        var high = Drill(intensity: DrillIntensity.High);
        var filter = new DrillFilterRequest { Intensities = [DrillIntensity.High] };

        var result = Run([low, high], q => q.ApplyAttributeFilters(filter));

        result.Should().ContainSingle().Which.Should().Be(high);
    }

    [Test]
    public void Search_matches_name_or_description_case_insensitively()
    {
        var byName = Drill(name: "Serving Ladder");
        var byDescription = Drill(name: "Drill", description: "great LADDER footwork");
        var noMatch = Drill(name: "Pancake", description: "dig practice");
        var filter = new DrillFilterRequest { SearchTerm = "ladder" };

        var result = Run([byName, byDescription, noMatch], q => q.ApplyAttributeFilters(filter));

        result.Should().BeEquivalentTo([byName, byDescription]);
    }

    [Test]
    public void Equipment_filter_requires_all_supplied_names()
    {
        var conesOnly = Drill(equipment: ["Cones"]);
        var conesAndBalls = Drill(equipment: ["Cones", "Balls"]);
        var filter = new DrillFilterRequest { Equipment = ["Cones", "Balls"] };

        var result = Run([conesOnly, conesAndBalls], q => q.ApplyAttributeFilters(filter));

        result.Should().ContainSingle().Which.Should().Be(conesAndBalls);
    }

    [Test]
    public void Attribute_filters_combine_with_AND_across_dimensions()
    {
        var match = Drill(category: DrillCategory.Game, skills: [DrillSkill.Serving]);
        var wrongSkill = Drill(category: DrillCategory.Game, skills: [DrillSkill.Blocking]);
        var wrongCategory = Drill(category: DrillCategory.Warmup, skills: [DrillSkill.Serving]);
        var filter = new DrillFilterRequest
        {
            Categories = [DrillCategory.Game],
            Skills = [DrillSkill.Serving],
        };

        var result = Run([match, wrongSkill, wrongCategory], q => q.ApplyAttributeFilters(filter));

        result.Should().ContainSingle().Which.Should().Be(match);
    }
}
