using NUnit.Framework;
using Shared.Testing.Time;

namespace Shared.Testing.Base;

/// <summary>
/// Base class for all unit tests providing common functionality:
/// - TestTimeProvider for deterministic time-based tests
/// - Consistent test categorization
/// </summary>
[TestFixture]
[Category("Unit")]
public abstract class UnitTestBase
{
    /// <summary>
    /// A frozen time provider for deterministic tests.
    /// Time is frozen at 2024-06-15 12:00:00 UTC by default.
    /// Use TimeProvider.Advance*() methods to move time forward in tests.
    /// </summary>
    protected TestTimeProvider TimeProvider { get; private set; } = null!;

    /// <summary>
    /// Gets the current frozen time as DateTime (UTC).
    /// </summary>
    protected DateTime Now => TimeProvider.GetUtcNow().UtcDateTime;

    [SetUp]
    public virtual void SetUp()
    {
        // Create a fresh frozen time provider for each test
        // This ensures tests are deterministic and isolated
        TimeProvider = TestTimeProvider.CreateFrozen();
    }

    [TearDown]
    public virtual void TearDown()
    {
        // Override in derived classes for common cleanup
    }

    #region Time Helpers

    /// <summary>
    /// Gets a DateTime in the future (relative to the frozen time).
    /// </summary>
    protected DateTime FutureDate(int days = 7, int hours = 0)
    {
        return Now.AddDays(days).AddHours(hours);
    }

    /// <summary>
    /// Gets a DateTime in the past (relative to the frozen time).
    /// </summary>
    protected DateTime PastDate(int days = 1, int hours = 0)
    {
        return Now.AddDays(-days).AddHours(-hours);
    }

    /// <summary>
    /// Advances the frozen time by the specified duration.
    /// Useful for testing time-dependent behavior.
    /// </summary>
    protected void AdvanceTime(TimeSpan duration)
    {
        TimeProvider.Advance(duration);
    }

    /// <summary>
    /// Advances the frozen time by the specified number of days.
    /// </summary>
    protected void AdvanceTimeDays(int days)
    {
        TimeProvider.AdvanceDays(days);
    }

    /// <summary>
    /// Advances the frozen time by the specified number of hours.
    /// </summary>
    protected void AdvanceTimeHours(int hours)
    {
        TimeProvider.AdvanceHours(hours);
    }

    #endregion
}