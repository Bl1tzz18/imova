using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.GetEndedListing;

// Null unless the listing exists and has ended (EndedListingStatuses) — a draft, one in review, a
// rejected or a suspended listing stays invisible, as if it didn't exist.
public record GetEndedListingQuery(Guid ListingId) : IRequest<EndedListingDto?>;
