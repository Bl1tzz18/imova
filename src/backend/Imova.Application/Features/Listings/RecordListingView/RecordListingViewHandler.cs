using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings.Visitors;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.RecordListingView;

public class RecordListingViewHandler(IApplicationDbContext dbContext, IListingCounters counters, TimeProvider timeProvider)
    : IRequestHandler<RecordListingViewCommand>
{
    public async Task Handle(RecordListingViewCommand request, CancellationToken cancellationToken)
    {
        var visitor = ListingVisitors.Hash(request.UserId, request.VisitorId);
        if (visitor is null)
        {
            return;
        }

        var ownerUserId = await (
                from l in dbContext.Listings.AsNoTracking().WherePublic(dbContext)
                join p in dbContext.Publishers.AsNoTracking() on l.PublisherId equals p.Id
                where l.Id == request.ListingId
                select (Guid?)p.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerUserId is null || ownerUserId == request.UserId)
        {
            return;
        }

        await counters.TryCountAsync(request.ListingId, ListingCounter.View, visitor, timeProvider.GetUtcNow(), cancellationToken);
    }
}
