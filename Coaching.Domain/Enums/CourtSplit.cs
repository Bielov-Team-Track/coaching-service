namespace Coaching.Domain.Enums;

/// <summary>
/// How a court is divided for a session. Not a count of places: a court divided into
/// halves has three — the whole surface and the two halves.
/// </summary>
public enum CourtSplit
{
    Full = 0,
    Halves = 1,
    Quarters = 2,
}

/// <summary>
/// The one place that decides which zones a split offers. A court's whole surface is always
/// a place — that is the null zone — and the divisions sit inside it. Callers ask here rather
/// than re-testing the enum, so adding a split is a change in a single file.
/// </summary>
public static class CourtZones
{
    public const string Left = "L";
    public const string Right = "R";
    public const string LeftNear = "LN";
    public const string LeftFar = "LF";
    public const string RightNear = "RN";
    public const string RightFar = "RF";

    private static readonly string[] Halves = [Left, Right];
    private static readonly string[] Quarters = [LeftNear, LeftFar, RightNear, RightFar];

    /// <summary>The divisions a split offers, not counting the whole surface.</summary>
    public static IReadOnlyList<string> For(CourtSplit split) => split switch
    {
        CourtSplit.Halves => Halves,
        CourtSplit.Quarters => Quarters,
        _ => [],
    };

    /// <summary>
    /// True when the zone is a place on a court divided this way. Null is the whole surface,
    /// which every split keeps.
    /// </summary>
    public static bool Allows(CourtSplit split, string? zoneId) =>
        zoneId is null || For(split).Contains(zoneId);
}
