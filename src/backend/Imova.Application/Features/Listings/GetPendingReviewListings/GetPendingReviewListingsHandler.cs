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

        var pendingReviewQuery = dbContext.Listings
            .AsNoTracking()
            .Where(l => l.Status == ListingStatus.PendingReview);

        // Oldest submission first — a fair, first-in-first-out review queue.
        var totalCount = await pendingReviewQuery.CountAsync(cancellationToken);
        var listings = await pendingReviewQuery
            .OrderBy(l => l.UpdatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = await ListingDtoLoader.LoadAsync(dbContext, blobStorageService, listings, currentUserId: null, cancellationToken);
        return new PagedResult<ListingDto>(items, request.Page, request.PageSize, totalCount);
    }
}
