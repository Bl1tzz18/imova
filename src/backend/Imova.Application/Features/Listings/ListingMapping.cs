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
        bool isSaved) =>
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
            isSaved);

    public static ListingDto ToDto(
        this Listing listing,
        Property property,
        PropertyLocation? location,
        Publisher publisher,
        IReadOnlyDictionary<Guid, Amenity> amenitiesById,
        IReadOnlyDictionary<Guid, Proximity> proximitiesById,
        IReadOnlyList<PhotoDto> photos,
        bool isSaved,
        bool includeContactDetails) =>
        listing.ToDto(
            property.ToDto(location, amenitiesById, proximitiesById),
            publisher.ToDto(includeContactDetails),
            photos,
            isSaved);

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
