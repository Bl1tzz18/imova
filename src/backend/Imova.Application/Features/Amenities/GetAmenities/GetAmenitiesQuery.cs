using Imova.Contracts.Amenities;
using MediatR;

namespace Imova.Application.Features.Amenities.GetAmenities;

public record GetAmenitiesQuery : IRequest<List<AmenityDto>>;
