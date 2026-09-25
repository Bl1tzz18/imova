using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.GetConversations;

// The caller's inbox, most recent activity first. Archived = show the archive instead. Search
// matches the other participant's name or the listing title.
public record GetConversationsQuery(Guid UserId, string? Search = null, bool Archived = false)
    : IRequest<List<ConversationSummaryDto>>;
