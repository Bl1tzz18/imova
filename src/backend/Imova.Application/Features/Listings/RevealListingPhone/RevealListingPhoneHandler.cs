using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings.Visitors;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.RevealListingPhone;

public class RevealListingPhoneHandler(IApplicationDbContext dbContext, IListingCounters counters, TimeProvider timeProvider)
    : IRequestHandler<RevealListingPhoneCommand, ListingPhoneDto?>
{
    public async Task<ListingPhoneDto?> Handle(RevealListingPhoneCommand request, CancellationToken cancellationToken)
    {
        var row = await (
                from l in dbContext.Listings.AsNoTracking()
                join p in dbContext.Publishers.AsNoTracking() on l.PublisherId equals p.Id
                where l.Id == request.ListingId && l.Status == ListingStatus.Active
                select new { Listing = l, Publisher = p })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var isOwner = request.UserId is not null && row.Publisher.UserId == request.UserId;
        // The same number as the contact card; one the owner hid stays hidden from everyone else.
        var (phone, hidden) = row.Listing.ContactPhone(row.Publisher);
        if (string.IsNullOrWhiteSpace(phone) || (hidden && !isOwner && !request.IsAdmin))
        {
            return null;
        }

        if (!isOwner && ListingVisitors.Hash(request.UserId, request.VisitorId) is { } visitor)
        {
            await counters.TryCountAsync(row.Listing.Id, ListingCounter.PhoneReveal, visitor, timeProvider.GetUtcNow(), cancellationToken);
        }

        return new ListingPhoneDto(phone);
    }
}
