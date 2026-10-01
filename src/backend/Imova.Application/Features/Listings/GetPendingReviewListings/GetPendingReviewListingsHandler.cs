using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.GetPendingReviewListings;

public class GetPendingReviewListingsHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetPendingReviewListingsQuery, PagedResult<ListingDto>>
{
    public async Task<PagedResult<ListingDto>> Handle(GetPendingReviewListingsQuery request, CancellationToken cancellationToken)
    {
        if (!request.IsAdmin)
        {
            throw new ForbiddenAccessException();
        }

        var query = dbContext.Listings
            .AsNoTracking()
            .Where(l => l.Status == request.Status);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            if (ListingIdIn(term) is { } listingId)
            {
                query = query.Where(l => l.Id == listingId);
            }
            else if (ListingNumberIn(term) is { } number)
            {
                query = query.Where(l => l.Number == number);
            }
            else
            {
                var lower = term.ToLower();
                query =
                    from l in query
                    join p in dbContext.Publishers on l.PublisherId equals p.Id
                    where l.Title.ToLower().Contains(lower)
                        || p.DisplayName.ToLower().Contains(lower)
                        || p.Email.ToLower().Contains(lower)
                    select l;
            }
        }

        // The review queue: oldest submission first (first in, first out). Active/Suspended: most
        // recently changed first. Id breaks ties so pages are stable.
        var ordered = request.Status == ListingStatus.PendingReview
            ? query.OrderBy(l => l.UpdatedAt).ThenBy(l => l.Id)
            : query.OrderByDescending(l => l.UpdatedAt).ThenBy(l => l.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var listings = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = await ListingDtoLoader.LoadAsync(
            dbContext, blobStorageService, listings, currentUserId: null, cancellationToken,
            // Admin-only route (RequireAdmin): the rows show each listing's views and phone reveals.
            viewerIsAdmin: true);
        return new PagedResult<ListingDto>(items, request.Page, request.PageSize, totalCount);
    }

    // "https://imova.md/property/<id>", "<id>" — the admin usually has the listing's link at hand.
    // The listing's short public number, as a visitor would quote it: "100015" or "ID 100015".
    private static long? ListingNumberIn(string term)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            term, @"^(?:id\s*)?(\d{6,18})$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success && long.TryParse(match.Groups[1].Value, out var number) ? number : null;
    }

    private static Guid? ListingIdIn(string term)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            term, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        return match.Success ? Guid.Parse(match.Value) : null;
    }
}
