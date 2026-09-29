using Imova.Application.Features.Listings;
using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Listings;

public class ListingRemovalTests
{
    [Fact]
    public async Task RemoveAsync_KeepsAPropertyAnotherListingStillUses()
    {
        var db = TestDbContextFactory.Create();
        var publisher = ListingTestData.AddIndividualPublisher(db);
        var sale = ListingTestData.AddListing(db, publisher.Id, TransactionType.Sale);
        var rental = ListingTestData.AddListing(db, publisher.Id, propertyId: sale.PropertyId);
        await db.SaveChangesAsync();

        await ListingRemoval.RemoveAsync(db, [sale], CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(await db.Properties.AnyAsync(p => p.Id == sale.PropertyId));
        Assert.True(await db.Listings.AnyAsync(l => l.Id == rental.Id));
    }

    [Fact]
    public async Task RemoveAsync_OfEveryListingOfAProperty_InOneBatch_RemovesThePropertyAndLocation()
    {
        var db = TestDbContextFactory.Create();
        var publisher = ListingTestData.AddIndividualPublisher(db);
        var sale = ListingTestData.AddListing(db, publisher.Id, TransactionType.Sale);
        var rental = ListingTestData.AddListing(db, publisher.Id, propertyId: sale.PropertyId);
        var photo = Photo.Create(rental.Id, "rental/1.jpg", "image/jpeg", 100);
        db.Photos.Add(photo);
        await db.SaveChangesAsync();

        var blobs = await ListingRemoval.RemoveAsync(db, [sale, rental], CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.False(await db.Listings.AnyAsync());
        Assert.False(await db.Properties.AnyAsync());
        Assert.False(await db.PropertyLocations.AnyAsync());
        Assert.False(await db.Photos.AnyAsync());
        Assert.Equal(["rental/1.jpg"], blobs);
    }
}
