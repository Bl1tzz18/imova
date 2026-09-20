using Imova.Application.Features.Locations.GetRaionLocalitati;
using Imova.Domain.Locations;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Locations;

public class GetRaionLocalitatiHandlerTests
{
    [Fact]
    public async Task Handle_ForKnownRaion_ReturnsOnlyItsLocalitatiOrderedByName()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var raion = Raion.Create(Guid.NewGuid(), "5500", "Ialoveni", null, LocalityLabel.Localitate);
        var otherRaion = Raion.Create(Guid.NewGuid(), "0300", "Bălți", null, LocalityLabel.Localitate);
        dbContext.Raioane.AddRange(raion, otherRaion);
        dbContext.Localitati.AddRange(
            Localitate.Create(Guid.NewGuid(), raion.Id, null, "5502", "Bardar", null),
            Localitate.Create(Guid.NewGuid(), raion.Id, null, "5501", "Alexandrovca", null),
            Localitate.Create(Guid.NewGuid(), otherRaion.Id, null, "0301", "Centru", null));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetRaionLocalitatiHandler(dbContext);
        var result = await handler.Handle(new GetRaionLocalitatiQuery(raion.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(["Alexandrovca", "Bardar"], result!.Select(l => l.NameRo));
    }

    [Fact]
    public async Task Handle_ForUnknownRaionId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetRaionLocalitatiHandler(dbContext);

        var result = await handler.Handle(new GetRaionLocalitatiQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ForRaionWithNoLocalitati_ReturnsEmptyList()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var raion = Raion.Create(Guid.NewGuid(), "9600", "UTA Găgăuzia", null, LocalityLabel.Localitate);
        dbContext.Raioane.Add(raion);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetRaionLocalitatiHandler(dbContext);
        var result = await handler.Handle(new GetRaionLocalitatiQuery(raion.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!);
    }
}
