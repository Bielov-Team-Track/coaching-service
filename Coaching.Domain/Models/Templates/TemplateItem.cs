using Coaching.Domain.Enums;
using Coaching.Domain.Models.Drills;
using Shared.Models;

namespace Coaching.Domain.Models.Templates;

public class PlanItem : BaseEntity
{
    public const int NotesMaxLength = 500;
    public const int TitleMaxLength = 200;

    public Guid TemplateId { get; set; }
    public ItemKind Kind { get; set; } = ItemKind.Drill;

    /// <summary>Set only when <see cref="Kind"/> is <see cref="ItemKind.Drill"/>.</summary>
    public Guid? DrillId { get; set; }

    /// <summary>The row's own name, for the kinds that have no drill to borrow one from.</summary>
    public string? Title { get; set; }

    public Guid? SectionId { get; set; }
    public int Order { get; set; }
    public int Duration { get; set; }

    /// <summary>
    /// The one free-text line under the drill. CJ renamed this column from "Notes" to
    /// "Goal/Focus" mid-season and started writing intent instead of setup — so this is
    /// the goal, and the label is what makes it one.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Stations only: the length the coach asked for. <see cref="Duration"/> is that or the
    /// longest group, whichever is greater — kept apart so a group that temporarily outgrows
    /// the block does not overwrite the intent.
    /// </summary>
    public int? PlannedDuration { get; set; }

    // Navigation properties
    public virtual TrainingPlan Plan { get; set; } = null!;
    public virtual PlanSection? Section { get; set; }
    public virtual Drill? Drill { get; set; }

    /// <summary>The groups running side by side. Only a Stations row has any.</summary>
    public virtual ICollection<PlanStation> Stations { get; set; } = new List<PlanStation>();
}
