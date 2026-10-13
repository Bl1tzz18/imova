using Imova.Application.Features.Listings;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Listings;

public class ListingDtoLoaderTests
{
    // Search (or "Anunțuri asemănătoare") loaded two listings; before their cards are built, the owner
    // of one deletes it — its Property goes with it. The answer is the other card, not a 500.
    [Fact]
    public async Task AListingDeletedMeanwhile_IsLeftOut_NotAnError()
    {
        await using var db = TestDbContextFactory.Create();
        var owner = Guid.NewGuid();
        ListingTestData.AddUser(db, owner);
        var publisher = ListingTestData.AddIndividualPublisher(db, owner);
        var kept = ListingTestData.AddListing(db, publisher.Id);
        var deleted = ListingTestData.AddListing(db, publisher.Id);
        await db.SaveChangesAsync();
        var loaded = await db.Listings.AsNoTracking().OrderBy(l => l.Id == kept.Id ? 0 : 1).ToListAsync();

        db.Listings.Remove(deleted);
        await db.SaveChangesAsync();
        db.Properties.Remove(await db.Properties.SingleAsync(p => p.Id == deleted.PropertyId));
        await db.SaveChangesAsync();

        var cards = await ListingDtoLoader.LoadAsync(db, new FakeBlobStorageService(), loaded, null, CancellationToken.None);

        Assert.Equal(kept.Id, Assert.Single(cards).Id);
    }
}
