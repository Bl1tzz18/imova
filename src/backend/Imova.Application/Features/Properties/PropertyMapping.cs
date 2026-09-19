using Imova.Application.Common.Identity;
using Imova.Contracts.Media;
using Imova.Contracts.Properties;
using Imova.Domain.Locations;
using Imova.Domain.Properties;

namespace Imova.Application.Features.Properties;

public static class PropertyMapping
{
    public static PropertyDto ToDto(
        this Property property,
        PropertyLocation? location,
        ApplicationUser? owner = null,
        IReadOnlyList<PropertyMediaDto>? media = null,
        bool isSaved = false) =>
        new(
            property.Id,
            property.OwnerId,
            property.OrganizationId,
            property.Title,
            property.Description,
            property.PropertyType.ToString(),
            property.ListingType.ToString(),
            property.Status.ToString(),
            property.Price,
            property.Currency,
            property.Area,
            property.Rooms,
            property.Bathrooms,
            property.Floor,
            property.TotalFloors,
            property.YearBuilt,
            property.Furnished,
            property.ParkingAvailable,
            property.PetsAllowed,
            property.CreatedAt,
            property.UpdatedAt,
            property.PublishedAt,
            property.ExpiresAt,
            property.RejectionReason,
            property.SuspensionReason,
            location is null
                ? null
                : new PropertyLocationDto(
                    location.Country,
                    location.Region,
                    location.City,
                    location.District,
                    location.Sector,
                    location.Street,
                    location.BuildingNumber,
                    location.Latitude,
                    location.Longitude),
            owner is null ? null : new PropertyOwnerDto(owner.Email ?? string.Empty, owner.PhoneNumber),
            media ?? [],
            isSaved);
}
