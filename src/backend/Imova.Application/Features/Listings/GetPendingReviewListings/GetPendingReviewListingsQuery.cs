using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.GetPendingReviewListings;

// The admin moderation lists: the review queue (PendingReview, the default), and Active / Suspended
// listings to suspend or reinstate (see AdminListingStatuses), optionally narrowed by Search. IsAdmin is always supplied by the endpoint
// from the caller's JWT claims — never trust it from the request body/query string.
public record GetPendingReviewListingsQuery(
    bool IsAdmin,
    int Page = 1,
    int PageSize = 20,
    ListingStatus Status = ListingStatus.PendingReview,
    // Finds one listing among many: a listing id — or a pasted listing URL containing one — or text
    // matched (case-insensitively) against the title and the publisher's name and email.
    string? Search = null)
    : IRequest<PagedResult<ListingDto>>;

public static class AdminListingStatuses
{
    public static readonly IReadOnlySet<ListingStatus> Allowed =
        new HashSet<ListingStatus> { ListingStatus.PendingReview, ListingStatus.Active, ListingStatus.Suspended };
}
