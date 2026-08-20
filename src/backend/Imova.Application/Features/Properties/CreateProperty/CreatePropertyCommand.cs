using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.CreateProperty;

// OwnerId is supplied by the caller for now — there is no auth yet, so there's no
// authenticated-user context to pull it from. Once auth exists, this should come from
// the caller's claims instead of the request body.
public record CreatePropertyCommand(
    Guid OwnerId,
    string Title,
    string Description,
    PropertyType PropertyType,
    ListingType ListingType,
    decimal Price,
    string Currency,
    string Country,
    string City,
    string? District,
    double Latitude,
    double Longitude) : IRequest<PropertyDto>;
