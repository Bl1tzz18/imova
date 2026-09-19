using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.GetMyProperties;

// Unlike GetPropertiesQuery (public listings, filtered by type), this returns every status —
// Draft, Published, Archived, … — since the owner needs to see and manage all of their own
// listings, not just the ones visible to other visitors.
public record GetMyPropertiesQuery(Guid OwnerId) : IRequest<List<PropertyDto>>;
