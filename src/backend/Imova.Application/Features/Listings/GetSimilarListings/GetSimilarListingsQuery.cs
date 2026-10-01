using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.GetSimilarListings;

// "Anunțuri asemănătoare" under a listing: up to SimilarListingStages.MaxResults other Active
// listings of the same transaction and property type, the closest first — for an Active listing, and
// for one that has ended (sold, rented, …), whose page offers alternatives. Null otherwise (a draft,
// in review, rejected, suspended, or no such listing): the endpoint answers 404.
public record GetSimilarListingsQuery(Guid ListingId, Guid? CurrentUserId) : IRequest<IReadOnlyList<ListingDto>?>;
