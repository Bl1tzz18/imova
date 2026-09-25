using Imova.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Messaging.GetConversationIdForListing;

public class GetConversationIdForListingHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetConversationIdForListingQuery, Guid?>
{
    public async Task<Guid?> Handle(GetConversationIdForListingQuery request, CancellationToken cancellationToken) =>
        await dbContext.Conversations
            .Where(c => c.ListingId == request.ListingId && c.InitiatorUserId == request.UserId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
