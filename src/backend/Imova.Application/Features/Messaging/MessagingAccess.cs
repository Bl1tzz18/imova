using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Messaging;

public static class MessagingAccess
{
    // The conversation, or null when it doesn't exist *or* the user isn't in it (both become 404,
    // so a conversation id reveals nothing to outsiders).
    public static async Task<Conversation?> FindForParticipantAsync(
        IApplicationDbContext dbContext, Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        return conversation is not null && conversation.IsParticipant(userId) ? conversation : null;
    }

    public static Task<bool> IsBlockedAsync(
        IApplicationDbContext dbContext, Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
        dbContext.UserBlocks.AnyAsync(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId, cancellationToken);

    // A banned sender, or one the recipient has blocked, can't send anything.
    public static async Task EnsureCanSendAsync(
        IApplicationDbContext dbContext, Guid senderUserId, Guid recipientUserId, CancellationToken cancellationToken)
    {
        var banned = await dbContext.Users.AnyAsync(u => u.Id == senderUserId && u.IsBannedFromMessaging, cancellationToken);
        if (banned)
        {
            throw new ForbiddenAccessException("You have been banned from sending messages.");
        }

        if (await IsBlockedAsync(dbContext, recipientUserId, senderUserId, cancellationToken))
        {
            throw new ForbiddenAccessException("This user has blocked you — you can't send them messages.");
        }
    }

    public static Task<int> UnreadCountAsync(IApplicationDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
        (from m in dbContext.Messages
         join c in dbContext.Conversations on m.ConversationId equals c.Id
         where (c.InitiatorUserId == userId || c.PublisherUserId == userId) && m.SenderUserId != userId && m.ReadAt == null
         select m.Id).CountAsync(cancellationToken);
}
