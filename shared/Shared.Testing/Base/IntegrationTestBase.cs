using NUnit.Framework;
using Shared.Testing.Fixtures;

namespace Shared.Testing.Base;

[TestFixture]
[Category("Integration")]
public abstract class IntegrationTestBase
{
    protected static PostgresFixture PostgresFixture { get; private set; } = null!;
    protected DatabaseResetter DatabaseResetter { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpBase()
    {
        PostgresFixture = new PostgresFixture();
        await PostgresFixture.InitializeAsync();

        DatabaseResetter = new DatabaseResetter(PostgresFixture.ConnectionString);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownBase()
    {
        await PostgresFixture.DisposeAsync();
    }

    [TearDown]
    public async Task TearDownBase()
    {
        await DatabaseResetter.ResetAsync();
    }
}