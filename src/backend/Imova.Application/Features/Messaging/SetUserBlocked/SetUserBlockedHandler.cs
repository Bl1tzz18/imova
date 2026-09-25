using Imova.Application.Common.Interfaces;
using Imova.Domain.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Messaging.SetUserBlocked;

public class SetUserBlockedHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<SetUserBlockedCommand, bool>
{
    public async Task<bool> Handle(SetUserBlockedCommand request, CancellationToken cancellationToken)
    {
        var conversation = await MessagingAccess.FindForParticipantAsync(dbContext, request.ConversationId, request.UserId, cancellationToken);
        if (conversation is null)
        {
            return false;
        }

        var otherUserId = conversation.OtherParticipant(request.UserId);
        var existing = await dbContext.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerUserId == request.UserId && b.BlockedUserId == otherUserId, cancellationToken);

        if (request.Blocked && existing is null)
        {
            dbContext.UserBlocks.Add(new UserBlock(request.UserId, otherUserId, timeProvider.GetUtcNow()));
        }
        else if (!request.Blocked && existing is not null)
        {
            dbContext.UserBlocks.Remove(existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
