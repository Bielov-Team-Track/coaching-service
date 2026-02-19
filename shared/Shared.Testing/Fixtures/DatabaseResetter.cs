using Npgsql;
using Respawn;

namespace Shared.Testing.Fixtures;

public class DatabaseResetter
{
    private Respawner? _respawner;
    private readonly string _connectionString;

    public DatabaseResetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Lazily initialize Respawner on first reset (after migrations have run)
        _respawner ??= await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" }
        });

        await _respawner.ResetAsync(conn);
    }
}