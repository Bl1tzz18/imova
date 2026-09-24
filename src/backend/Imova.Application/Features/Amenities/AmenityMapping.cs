using Imova.Contracts.Amenities;
using Imova.Domain.Amenities;

namespace Imova.Application.Features.Amenities;

public static class AmenityMapping
{
    public static AmenityDto ToDto(this Amenity amenity) => new(amenity.Id, amenity.Key, amenity.LabelRo, amenity.Category.ToString());
}
