namespace Shared.Testing.Time;

/// <summary>
/// A time provider for tests that allows freezing and advancing time.
/// Uses the built-in .NET TimeProvider as base.
/// </summary>
public sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;
    private readonly TimeZoneInfo _localTimeZone;

    /// <summary>
    /// Creates a new TestTimeProvider with the current time frozen.
    /// </summary>
    public TestTimeProvider() : this(DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Creates a new TestTimeProvider with time frozen at the specified moment.
    /// </summary>
    /// <param name="startTime">The time to freeze at.</param>
    public TestTimeProvider(DateTimeOffset startTime)
    {
        _utcNow = startTime;
        _localTimeZone = TimeZoneInfo.Local;
    }

    /// <summary>
    /// Creates a new TestTimeProvider with time frozen at the specified moment.
    /// </summary>
    /// <param name="startTime">The time to freeze at (will be converted to UTC).</param>
    public TestTimeProvider(DateTime startTime)
        : this(startTime.Kind == DateTimeKind.Utc
            ? new DateTimeOffset(startTime)
            : new DateTimeOffset(startTime.ToUniversalTime()))
    {
    }

    /// <summary>
    /// Gets the current frozen time.
    /// </summary>
    public override DateTimeOffset GetUtcNow() => _utcNow;

    /// <summary>
    /// Gets the local time zone.
    /// </summary>
    public override TimeZoneInfo LocalTimeZone => _localTimeZone;

    /// <summary>
    /// Sets the current time to a specific value.
    /// </summary>
    public void SetUtcNow(DateTimeOffset value) => _utcNow = value;

    /// <summary>
    /// Sets the current time to a specific value.
    /// </summary>
    public void SetUtcNow(DateTime value)
    {
        _utcNow = value.Kind == DateTimeKind.Utc
            ? new DateTimeOffset(value)
            : new DateTimeOffset(value.ToUniversalTime());
    }

    /// <summary>
    /// Advances the current time by the specified duration.
    /// </summary>
    public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);

    /// <summary>
    /// Advances the current time by the specified number of days.
    /// </summary>
    public void AdvanceDays(int days) => Advance(TimeSpan.FromDays(days));

    /// <summary>
    /// Advances the current time by the specified number of hours.
    /// </summary>
    public void AdvanceHours(int hours) => Advance(TimeSpan.FromHours(hours));

    /// <summary>
    /// Advances the current time by the specified number of minutes.
    /// </summary>
    public void AdvanceMinutes(int minutes) => Advance(TimeSpan.FromMinutes(minutes));

    /// <summary>
    /// Advances the current time by the specified number of seconds.
    /// </summary>
    public void AdvanceSeconds(int seconds) => Advance(TimeSpan.FromSeconds(seconds));

    /// <summary>
    /// Creates a time provider frozen at a specific point in time for testing.
    /// Common preset: "now" is 2024-06-15 12:00:00 UTC (a Saturday).
    /// </summary>
    public static TestTimeProvider CreateFrozen() =>
        new(new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// Creates a time provider frozen at a weekday (Monday) for testing business logic.
    /// </summary>
    public static TestTimeProvider CreateFrozenWeekday() =>
        new(new DateTimeOffset(2024, 6, 17, 9, 0, 0, TimeSpan.Zero)); // Monday

    /// <summary>
    /// Creates a time provider frozen at a weekend (Saturday) for testing weekend logic.
    /// </summary>
    public static TestTimeProvider CreateFrozenWeekend() =>
        new(new DateTimeOffset(2024, 6, 15, 14, 0, 0, TimeSpan.Zero)); // Saturday
}
