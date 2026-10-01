using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.GetSimilarListings;

// "Anunțuri asemănătoare" under a listing: up to SimilarListingStages.MaxResults other Active
// listings of the same transaction and property type, the closest first. Null when the listing
// doesn't exist or isn't Active (the endpoint answers 404).
public record GetSimilarListingsQuery(Guid ListingId, Guid? CurrentUserId) : IRequest<IReadOnlyList<ListingDto>?>;
