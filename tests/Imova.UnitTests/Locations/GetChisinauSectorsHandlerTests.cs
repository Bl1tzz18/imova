using Imova.Application.Features.Locations.GetChisinauSectors;
using Imova.Domain.Locations;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.UnitTests.Locations;

public class GetChisinauSectorsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllSectorsOrderedByName()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.ChisinauSectors.AddRange(
            ChisinauSector.Create("Râșcani"),
            ChisinauSector.Create("Botanica"),
            ChisinauSector.Create("Centru"));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetChisinauSectorsHandler(dbContext, new MemoryCache(new MemoryCacheOptions()));
        var result = await handler.Handle(new GetChisinauSectorsQuery(), CancellationToken.None);

        Assert.Equal(["Botanica", "Centru", "Râșcani"], result.Select(s => s.Name));
    }

    [Fact]
    public async Task Handle_WithNoSectors_ReturnsEmptyList()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetChisinauSectorsHandler(dbContext, new MemoryCache(new MemoryCacheOptions()));

        var result = await handler.Handle(new GetChisinauSectorsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_SecondCall_DoesNotReQueryTheDatabase()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.ChisinauSectors.Add(ChisinauSector.Create("Botanica"));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new GetChisinauSectorsHandler(dbContext, cache);
        var first = await handler.Handle(new GetChisinauSectorsQuery(), CancellationToken.None);

        // Dispose the context to prove the second call can't be hitting the database — if it
        // tried, EF Core would throw ObjectDisposedException instead of returning the cached list.
        await dbContext.DisposeAsync();
        var second = await handler.Handle(new GetChisinauSectorsQuery(), CancellationToken.None);

        Assert.Same(first, second);
    }
}
