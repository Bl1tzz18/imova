using Imova.Application.Features.Locations.GetRaioane;
using Imova.Domain.Locations;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Locations;

public class GetRaioaneHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllRaioaneOrderedByName()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Raioane.AddRange(
            Raion.Create(Guid.NewGuid(), "0300", "Bălți", null, LocalityLabel.Localitate),
            Raion.Create(Guid.NewGuid(), "0100", "Chișinău", null, LocalityLabel.Sector),
            Raion.Create(Guid.NewGuid(), "5500", "Ialoveni", null, LocalityLabel.Localitate));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetRaioaneHandler(dbContext);
        var result = await handler.Handle(new GetRaioaneQuery(), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal(["Bălți", "Chișinău", "Ialoveni"], result.Select(r => r.NameRo));
    }

    [Fact]
    public async Task Handle_MapsLocalityLabelToItsStringName()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Raioane.Add(Raion.Create(Guid.NewGuid(), "0100", "Chișinău", null, LocalityLabel.Sector));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetRaioaneHandler(dbContext);
        var result = await handler.Handle(new GetRaioaneQuery(), CancellationToken.None);

        Assert.Equal("Sector", result.Single().LocalityLabel);
    }

    [Fact]
    public async Task Handle_WithNoRaioane_ReturnsEmptyList()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetRaioaneHandler(dbContext);

        var result = await handler.Handle(new GetRaioaneQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
