using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.GetPendingReviewListings;

// IsAdmin is always supplied by the endpoint from the caller's JWT claims — never trust it from
// the request body/query string.
public record GetPendingReviewListingsQuery(bool IsAdmin, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ListingDto>>;
