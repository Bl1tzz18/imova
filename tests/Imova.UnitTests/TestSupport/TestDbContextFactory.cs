using Imova.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.TestSupport;

internal static class TestDbContextFactory
{
    // A fresh, isolated database per call (unique name) — handlers under test see a real
    // ImovaDbContext/DbSet<T>, just backed by EF Core's InMemory provider instead of Postgres.
    public static ImovaDbContext Create() => Create(Guid.NewGuid().ToString());

    // Same name = same underlying store — lets a test save through one context and read back
    // through a fresh one (no change-tracker cache), to check what actually round-trips.
    public static ImovaDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ImovaDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ImovaDbContext(options);
    }
}
