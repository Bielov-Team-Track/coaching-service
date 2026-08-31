using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Drills;

/// <summary>
/// A dial as the drill defines it. The values belong to each use of the drill and travel with
/// the plan, not with this.
/// </summary>
public class DrillDialDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DialKind Kind { get; set; }
    public required string DefaultValue { get; set; }

    /// <summary>Toggle only: the sentences the instructions read in each state.</summary>
    public string? OnText { get; set; }
    public string? OffText { get; set; }

    /// <summary>Toggle only: the short words on the control itself.</summary>
    public string? OnLabel { get; set; }
    public string? OffLabel { get; set; }

    public int Order { get; set; }
}

/// <summary>
/// Promoting a word to a dial. The client has already spliced the token into the prose and
/// sends the result as <paramref name="InstructionsHtml"/>; the server checks the two agree
/// before it stores either.
/// </summary>
public record CreateDrillDialDto(
    string Name,
    DialKind Kind,
    string DefaultValue,
    string InstructionsHtml,
    string? OnText = null,
    string? OffText = null,
    string? OnLabel = null,
    string? OffLabel = null
);

/// <summary>
/// Changing a dial. Every field is optional and null means "leave it alone" — including
/// <paramref name="InstructionsHtml"/>, which only a rename needs to send.
/// </summary>
public record UpdateDrillDialDto(
    string? NewName = null,
    string? DefaultValue = null,
    string? OnText = null,
    string? OffText = null,
    string? OnLabel = null,
    string? OffLabel = null,
    string? InstructionsHtml = null
);

/// <summary>Removing a dial, with the prose the client has already put the words back into.</summary>
public record DeleteDrillDialDto(
    string InstructionsHtml
);

/// <summary>
/// Folding a duplicate drill into the one being kept: every plan that used the source now uses
/// the keeper, reading it through the dial values given here.
/// </summary>
public record FoldDrillDto(
    Guid SourceDrillId,
    Dictionary<string, string>? ValuesForSourceUses = null
);

public record FoldDrillResultDto(
    int MovedUses
);
