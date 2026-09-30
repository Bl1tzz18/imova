using Imova.Application.Common.Identity;
using Imova.Infrastructure;
using Imova.Infrastructure.Locations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Imova.IntegrationTests.TestSupport;

// Recreates the test database once per run, before the first test host starts: dropped, then
// migrated and seeded from scratch, so every run (locally and in CI) starts from the same clean
// state instead of piling up the users, listings and conversations earlier runs left behind. Every
// test class starts its own API host, and each host's startup (Program.cs) migrates, seeds and
// creates the roles — harmless on a ready database, but on a brand-new one a dozen hosts doing it
// at the same moment collide, hence doing it here first. Triggered through
// ListingApi.ConnectionString, which every integration test reads. (Not a [ModuleInitializer]:
// that holds the assembly's init lock, so the async work below — running this assembly's own code
// on another thread — would wait for it forever.)
internal static class TestDatabase
{
    // CI (or any other machine) can point the tests elsewhere; the fallback is the compose Postgres.
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("IMOVA_TEST_CONNECTION_STRING") is { Length: > 0 } configured
            ? configured
            : "Host=localhost;Port=5432;Database=imova_test;Username=imova;Password=imova";

    // Task.Run: blocking on async work under xUnit's synchronization context deadlocks.
    private static readonly Lazy<Task> Ready = new(() => Task.Run(PrepareAsync));

    public static void EnsureReady() => Ready.Value.GetAwaiter().GetResult();

    private static async Task PrepareAsync()
    {
        await DropDatabaseAsync();

        var options = new DbContextOptionsBuilder<ImovaDbContext>()
            .UseNpgsql(ConnectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var dbContext = new ImovaDbContext(options);

        await dbContext.Database.MigrateAsync();
        await CuatmLocationSeeder.SeedAsync(dbContext, CancellationToken.None);
        await ChisinauSectorSeeder.SeedAsync(dbContext, CancellationToken.None);

        foreach (var role in new[] { Roles.User, Roles.Admin })
        {
            var normalized = role.ToUpperInvariant();
            if (!await dbContext.Roles.AnyAsync(r => r.NormalizedName == normalized))
            {
                dbContext.Roles.Add(new IdentityRole<Guid>(role)
                {
                    Id = Guid.NewGuid(), NormalizedName = normalized, ConcurrencyStamp = Guid.NewGuid().ToString(),
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    // The whole database goes, so refuse anything that isn't obviously a test database — a
    // mistyped IMOVA_TEST_CONNECTION_STRING must never be able to wipe the dev (or any real) data.
    private static async Task DropDatabaseAsync()
    {
        var target = new NpgsqlConnectionStringBuilder(ConnectionString);
        var database = target.Database
            ?? throw new InvalidOperationException("The test connection string names no database.");
        if (!database.EndsWith("_test", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to drop \"{database}\": the integration-test database's name must end in \"_test\".");
        }

        // Connected to the maintenance database, since a database can't drop itself.
        var maintenance = new NpgsqlConnectionStringBuilder(ConnectionString) { Database = "postgres", Pooling = false };
        await using var connection = new NpgsqlConnection(maintenance.ConnectionString);
        await connection.OpenAsync();
        await using var drop = connection.CreateCommand();
        // FORCE ends leftover sessions (a debugger stopped mid-run, a psql window left open).
        drop.CommandText = $"DROP DATABASE IF EXISTS {new NpgsqlCommandBuilder().QuoteIdentifier(database)} WITH (FORCE)";
        await drop.ExecuteNonQueryAsync();
    }
}
