using Imova.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Messaging.GetConversationCounterparts;

public class GetConversationCounterpartsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetConversationCounterpartsQuery, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(GetConversationCounterpartsQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var conversations = dbContext.Conversations.AsNoTracking()
            .Where(c => c.InitiatorUserId == userId || c.PublisherUserId == userId);
        if (request.ConversationId is { } conversationId)
        {
            conversations = conversations.Where(c => c.Id == conversationId);
        }

        return await conversations
            .Select(c => c.InitiatorUserId == userId ? c.PublisherUserId : c.InitiatorUserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
