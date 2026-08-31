using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Drills;

/// <summary>
/// One word of a drill's instructions the coach may set per use. The drill owns the dial —
/// its name, kind and default — and every use of the drill owns a value for it, so the same
/// library drill reads "6 reps" in one plan and "10 reps" in another without being copied.
/// The name is also the token spliced into the instructions, which is why it is restricted
/// to the token grammar rather than free text.
/// </summary>
public class DrillDial : BaseEntity
{
    public const int NameMaxLength = 60;
    public const int ValueMaxLength = 500;
    public const int LabelMaxLength = 40;

    public Guid DrillId { get; set; }

    /// <summary>The token spliced into the instructions: <c>{name}</c>.</summary>
    public required string Name { get; set; }

    public DialKind Kind { get; set; }

    /// <summary>What a use reads as until the coach changes it. A Toggle stores "true" or "false".</summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>Toggle only: the full sentence the instructions read when the dial is on.</summary>
    public string? OnText { get; set; }

    /// <summary>Toggle only: the full sentence the instructions read when the dial is off.</summary>
    public string? OffText { get; set; }

    /// <summary>Toggle only: the short word on the control itself, not in the prose.</summary>
    public string? OnLabel { get; set; }

    public string? OffLabel { get; set; }

    public int Order { get; set; }

    // Navigation properties
    public virtual Drill? Drill { get; set; }
}
