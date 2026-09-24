using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Listings;

// Saves through one context and reads back through a fresh one, so what's asserted is what the
// EF mapping (JSON converters, Price complex property, amenity join rows) actually round-trips.
public class ListingPersistenceTests
{
    [Fact]
    public async Task PropertyAndListing_RoundTripAllMappedValues()
    {
        var databaseName = Guid.NewGuid().ToString();
        var amenityId = Guid.NewGuid();
        var attributes = new HouseAttributes(
            Rooms: 5, LandAreaM2: 600m, HouseFloors: 2, ConstructionType: ConstructionType.Brick,
            Utilities: new HouseUtilities(Water: true, Sewage: false, Gas: true, Electricity: true));
        var rental = new RentalDetails(12, 1000m, true, FurnishedStatus.PartiallyFurnished, new DateTime(2026, 10, 1), true);
        Guid listingId;

        await using (var dbContext = TestDbContextFactory.Create(databaseName))
        {
            var publisher = ListingTestData.AddIndividualPublisher(dbContext);
            var property = ListingTestData.AddProperty(dbContext, PropertyType.House, attributes, [amenityId]);
            var listing = Listing.Create(
                property.Id, publisher.Id, TransactionType.Rent, "Casă", "Casă cu curte.",
                Price.Create(20_000m, Currency.MDL, true, 0.05m), rentalDetails: rental);
            dbContext.Listings.Add(listing);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            listingId = listing.Id;
        }

        await using (var dbContext = TestDbContextFactory.Create(databaseName))
        {
            var listing = await dbContext.Listings.SingleAsync(l => l.Id == listingId);
            var property = await dbContext.Properties.Include(p => p.Amenities).SingleAsync(p => p.Id == listing.PropertyId);

            Assert.Equal(attributes, property.TypeSpecificAttributes);
            Assert.Equal(amenityId, Assert.Single(property.Amenities).AmenityId);
            Assert.Equal(20_000m, listing.Price.Amount);
            Assert.Equal(Currency.MDL, listing.Price.Currency);
            Assert.Equal(1_000m, listing.Price.PriceEur);
            Assert.True(listing.Price.IsNegotiable);
            Assert.Equal(rental, listing.RentalDetails);
            Assert.Null(listing.SaleDetails);
        }
    }
}
