using Imova.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.TestSupport;

internal static class TestDbContextFactory
{
    // A fresh, isolated database per call (unique name) — handlers under test see a real
    // ImovaDbContext/DbSet<T>, just backed by EF Core's InMemory provider instead of Postgres.
    public static ImovaDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ImovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ImovaDbContext(options);
    }
}
