using Imova.Application.Common.Identity;
using Imova.Infrastructure;
using Imova.Infrastructure.Locations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Imova.IntegrationTests.TestSupport;

// Brings the test database up to date once, before the first test host starts. Every test class
// starts its own API host, and each host's startup (Program.cs) migrates, seeds and creates the
// roles — harmless on a ready database, but on a brand-new one (first run, a fresh machine, after
// `dropdb imova_test`) a dozen hosts doing it at the same moment collide. Triggered through
// ListingApi.ConnectionString, which every integration test reads. (Not a [ModuleInitializer]:
// that holds the assembly's init lock, so the async work below — running this assembly's own code
// on another thread — would wait for it forever.)
internal static class TestDatabase
{
    public const string ConnectionString = "Host=localhost;Port=5432;Database=imova_test;Username=imova;Password=imova";

    // Task.Run: blocking on async work under xUnit's synchronization context deadlocks.
    private static readonly Lazy<Task> Ready = new(() => Task.Run(PrepareAsync));

    public static void EnsureReady() => Ready.Value.GetAwaiter().GetResult();

    private static async Task PrepareAsync()
    {
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
}
