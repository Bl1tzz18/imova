using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Messaging.GetConversations;

public class GetConversationsHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetConversationsQuery, List<ConversationSummaryDto>>
{
    // An inbox is small; the search runs over the loaded rows (it needs the resolved names).
    private const int MaxConversations = 300;

    public async Task<List<ConversationSummaryDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var conversations = await dbContext.Conversations.AsNoTracking()
            .Where(c => (c.InitiatorUserId == userId && c.ArchivedByInitiator == request.Archived)
                || (c.PublisherUserId == userId && c.ArchivedByPublisher == request.Archived))
            .OrderByDescending(c => c.LastMessageAt)
            .Take(MaxConversations)
            .ToListAsync(cancellationToken);

        var summaries = await ConversationSummaries.LoadAsync(dbContext, blobStorageService, userId, conversations, cancellationToken);
        return summaries.Where(s => ConversationSummaries.Matches(s, request.Search)).ToList();
    }
}
