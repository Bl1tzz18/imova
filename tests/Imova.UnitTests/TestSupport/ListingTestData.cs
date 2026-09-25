using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;
using Imova.Domain.Publishers;
using Imova.Infrastructure;

namespace Imova.UnitTests.TestSupport;

// Builders for the Property + Listing + Publisher graph most listing tests need. Everything is
// added to the context but not saved — callers decide when to SaveChangesAsync.
internal static class ListingTestData
{
    public static readonly ApartmentAttributes TwoRoomApartment = new(Rooms: 2, Floor: 3, TotalFloors: 9);

    public static Price Eur(decimal amount, bool isNegotiable = false) =>
        Price.Create(amount, Currency.EUR, isNegotiable, eurRate: 1m);

    public static Publisher AddIndividualPublisher(ImovaDbContext dbContext, Guid? userId = null)
    {
        var publisher = Publisher.CreateIndividual(userId ?? Guid.NewGuid(), "Ion Popescu", "+373 69 123 456", "ion@example.com");
        dbContext.Publishers.Add(publisher);
        return publisher;
    }

    public static Publisher AddAgencyPublisher(ImovaDbContext dbContext, Guid userId)
    {
        var publisher = Publisher.CreateAgency(userId, "Imobil Grup", "+373 22 000 000", "office@imobil.md", null, null);
        dbContext.Publishers.Add(publisher);
        return publisher;
    }

    public static Property AddProperty(
        ImovaDbContext dbContext,
        PropertyType propertyType = PropertyType.Apartment,
        PropertyAttributes? attributes = null,
        IEnumerable<Guid>? amenityIds = null,
        IEnumerable<Guid>? proximityIds = null)
    {
        var location = PropertyLocation.Create(
            "Moldova", Guid.NewGuid(), "Chișinău", null, null, null, null, 47.0105, 28.8638, "Strada Ismail", "44");
        var property = Property.Create(
            propertyType,
            54m,
            propertyType == PropertyType.Land ? null : 2005,
            null,
            location.Id,
            attributes ?? (propertyType == PropertyType.Apartment ? TwoRoomApartment : PropertyAttributes.EmptyFor(propertyType)),
            amenityIds,
            proximityIds);
        dbContext.PropertyLocations.Add(location);
        dbContext.Properties.Add(property);
        return property;
    }

    // A Draft listing (no transitions applied) for a fresh property; see MoveTo to advance it.
    public static Listing AddListing(
        ImovaDbContext dbContext,
        Guid publisherId,
        TransactionType transactionType = TransactionType.Rent,
        Price? price = null,
        Guid? propertyId = null,
        PropertyType propertyType = PropertyType.Apartment,
        ListingContact? contact = null)
    {
        var listing = NewListing(
            propertyId ?? AddProperty(dbContext, propertyType).Id, publisherId, transactionType, price, contact);
        dbContext.Listings.Add(listing);
        return listing;
    }

    public static Listing NewListing(
        Guid? propertyId = null,
        Guid? publisherId = null,
        TransactionType transactionType = TransactionType.Rent,
        Price? price = null,
        ListingContact? contact = null) =>
        Listing.Create(
            propertyId ?? Guid.NewGuid(),
            publisherId ?? Guid.NewGuid(),
            transactionType,
            "Apartament 2 camere",
            "Apartament luminos, aproape de centru.",
            price ?? Eur(550m),
            rentalDetails: transactionType == TransactionType.Rent ? new RentalDetails() : null,
            contact: contact);

    // Walks a Draft listing through the real lifecycle methods to reach `target`, so tests never
    // depend on a status that couldn't actually be reached.
    public static Listing MoveTo(this Listing listing, ListingStatus target)
    {
        if (target == ListingStatus.Draft)
        {
            return listing;
        }

        listing.SubmitForReview();
        switch (target)
        {
            case ListingStatus.PendingReview:
                break;
            case ListingStatus.Rejected:
                listing.Reject("Poze neclare.");
                break;
            default:
                listing.Approve();
                switch (target)
                {
                    case ListingStatus.Active:
                        break;
                    case ListingStatus.Suspended:
                        listing.Suspend("Conținut duplicat.");
                        break;
                    case ListingStatus.Rented:
                        listing.MarkAsRented();
                        break;
                    case ListingStatus.Sold:
                        listing.MarkAsSold();
                        break;
                    case ListingStatus.Expired:
                        listing.Expire();
                        break;
                    case ListingStatus.Archived:
                        listing.Archive();
                        break;
                }

                break;
        }

        return listing;
    }
}
