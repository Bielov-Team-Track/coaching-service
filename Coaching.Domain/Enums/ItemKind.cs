namespace Coaching.Domain.Enums;

/// <summary>
/// What a row in a practice plan actually is. A break is not a drill with a
/// break-shaped name — it is its own kind, with no drill behind it.
/// </summary>
public enum ItemKind
{
    Drill = 0,
    Break = 1,
    Stations = 2,
    Meeting = 3,
}

/// <summary>
/// The one place that decides what a kind implies. Callers ask here rather than
/// re-testing the enum, so adding a kind is a change in a single file.
/// </summary>
public static class ItemKinds
{
    /// <summary>Time a coach is actually coaching. Breaks and meetings are not.</summary>
    public static bool IsCoached(this ItemKind kind) => kind is ItemKind.Drill or ItemKind.Stations;

    /// <summary>Only a Drill row points at a drill; every other kind carries its own title.</summary>
    public static bool HasDrill(this ItemKind kind) => kind is ItemKind.Drill;
}
