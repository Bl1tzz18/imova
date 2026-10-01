using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Amenities;
using Imova.Application.Features.Listings.Attributes;
using Imova.Application.Features.Proximities;
using Imova.Application.Features.Publishers;
using Imova.Contracts.Listings;
using Imova.Contracts.Properties;
using Imova.Contracts.Publishers;
using Imova.Domain.Amenities;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Domain.Proximities;
using Imova.Domain.Publishers;

namespace Imova.Application.Features.Listings;

public static class ListingMapping
{
    public static ListingDto ToDto(
        this Listing listing,
        PropertyDto property,
        PublisherDto publisher,
        IReadOnlyList<PhotoDto> photos,
        bool isSaved,
        ListingContactDto? contact = null) =>
        new(
            listing.Id,
            listing.Status.ToString(),
            listing.TransactionType.ToString(),
            listing.Title,
            listing.Description,
            listing.Price.ToDto(),
            listing.SaleDetails is null ? null : new SaleDetailsDto(listing.SaleDetails.OwnershipDocumentType),
            listing.RentalDetails?.ToDto(),
            listing.CreatedAt,
            listing.UpdatedAt,
            listing.PublishedAt,
            listing.ExpiresAt,
            listing.RejectionReason,
            listing.SuspensionReason,
            property,
            publisher,
            photos,
            isSaved,
            contact);

    public static ListingDto ToDto(
        this Listing listing,
        Property property,
        PropertyLocation? location,
        Publisher publisher,
        IReadOnlyDictionary<Guid, Amenity> amenitiesById,
        IReadOnlyDictionary<Guid, Proximity> proximitiesById,
        IReadOnlyList<PhotoDto> photos,
        bool isSaved,
        bool includeContactDetails,
        // The owner or an admin — they still see the contact's email and a phone number the owner
        // hid from the public.
        bool canSeePrivateDetails = false,
        // The account behind the publisher — only needed with contact details.
        PublisherPerson? publisherPerson = null) =>
        listing.ToDto(
            property.ToDto(location, amenitiesById, proximitiesById),
            // Never the publisher's own phone/email here: the listing's Contact is what the owner
            // chose to publish (possibly another number, or a hidden one).
            publisher.ToDto(includeContactDetails: false),
            photos,
            isSaved,
            includeContactDetails ? listing.ContactDto(publisher, canSeePrivateDetails, publisherPerson) : null);

    // Resolves a Self contact's name/email/photo from the publisher, and treats a listing from before
    // contact details existed as "Self, the publisher's own phone". The email is never public — the
    // listing page offers the phone, the apps and platform messages — only the owner and admins get it. An agency's Self contact is the
    // agent — the account behind the agency — shown by their own name and photo, with the agency
    // named beside them (falling back to the agency's name/logo while the account has none).
    public static ListingContactDto ContactDto(
        this Listing listing, Publisher publisher, bool canSeePrivateDetails, PublisherPerson? publisherPerson = null)
    {
        var isAgency = publisher.PublisherType == PublisherType.Agency;
        var agencyName = isAgency ? publisher.DisplayName : null;
        var selfName = isAgency && !string.IsNullOrWhiteSpace(publisherPerson?.DisplayName)
            ? publisherPerson.DisplayName
            : publisher.DisplayName;
        var selfPicture = publisherPerson?.PictureUrl ?? publisher.LogoUrl;

        var contact = listing.Contact;
        if (contact is null)
        {
            return new ListingContactDto(
                nameof(ContactPersonType.Self), selfName, publisher.Phone, canSeePrivateDetails ? publisher.Email : null, [],
                nameof(PreferredContactMethod.Any), false, null, null, selfPicture, agencyName);
        }

        var isSelf = contact.PersonType == ContactPersonType.Self;
        var phoneHidden = contact.HidePhoneNumber && !canSeePrivateDetails;
        return new ListingContactDto(
            contact.PersonType.ToString(),
            isSelf ? selfName : contact.Name,
            phoneHidden ? null : contact.Phone,
            !canSeePrivateDetails ? null : isSelf ? publisher.Email : contact.Email,
            (contact.MessagingApps ?? []).Select(a => a.ToString()).ToList(),
            contact.PreferredContactMethod.ToString(),
            contact.HidePhoneNumber,
            contact.CallHoursFrom,
            contact.CallHoursTo,
            isSelf ? selfPicture : null,
            agencyName);
    }

    public static PriceDto ToDto(this Price price) =>
        new(price.Amount, price.Currency.ToString(), price.PriceEur, price.IsNegotiable);

    public static RentalDetailsDto ToDto(this RentalDetails details) =>
        new(
            details.MinLeasePeriodMonths,
            details.SecurityDepositAmount,
            details.UtilitiesIncluded,
            details.AvailableFrom,
            details.PetsAllowed);

    public static PropertyDto ToDto(
        this Property property,
        PropertyLocation? location,
        IReadOnlyDictionary<Guid, Amenity> amenitiesById,
        IReadOnlyDictionary<Guid, Proximity> proximitiesById) =>
        new(
            property.Id,
            property.PropertyType.ToString(),
            property.TotalAreaM2,
            property.YearBuilt,
            property.Condition?.ToString(),
            PropertyAttributesJson.ToWireElement(property.TypeSpecificAttributes),
            property.Amenities
                .Select(a => amenitiesById.GetValueOrDefault(a.AmenityId))
                .OfType<Amenity>()
                .OrderBy(a => a.LabelRo, StringComparer.Ordinal)
                .Select(a => a.ToDto())
                .ToList(),
            // Seed order, same as GET /proximities.
            property.Proximities
                .Select(p => proximitiesById.GetValueOrDefault(p.ProximityId))
                .OfType<Proximity>()
                .OrderBy(p => p.Id)
                .Select(p => p.ToDto())
                .ToList(),
            location?.ToDto());

    public static PropertyLocationDto ToDto(this PropertyLocation location) =>
        new(
            location.Country,
            location.Region,
            location.RaionId,
            location.RaionName,
            location.LocalitateId,
            location.LocalitateName,
            location.ChisinauSectorId,
            location.ChisinauSectorName,
            location.Sector,
            location.Street,
            location.BuildingNumber,
            location.Latitude,
            location.Longitude);

    public static PhotoDto ToDto(this Photo photo, IBlobStorageService blobStorageService) =>
        new(
            photo.Id,
            photo.ListingId,
            blobStorageService.GetPublicUrl(photo.BlobName),
            photo.ContentType,
            photo.FileSizeBytes,
            photo.ModerationStatus.ToString(),
            photo.SortOrder,
            photo.IsPrimary,
            photo.CreatedAt);
}

// The account behind a publisher, as a listing's contact shows it: its own name and profile picture
// (a public URL of our own blob storage).
public sealed record PublisherPerson(string? DisplayName, string? PictureUrl);
