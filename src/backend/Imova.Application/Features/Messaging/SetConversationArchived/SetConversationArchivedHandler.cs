using Imova.Application.Common.Interfaces;
using MediatR;

namespace Imova.Application.Features.Messaging.SetConversationArchived;

public class SetConversationArchivedHandler(IApplicationDbContext dbContext) : IRequestHandler<SetConversationArchivedCommand, bool>
{
    public async Task<bool> Handle(SetConversationArchivedCommand request, CancellationToken cancellationToken)
    {
        var conversation = await MessagingAccess.FindForParticipantAsync(dbContext, request.ConversationId, request.UserId, cancellationToken);
        if (conversation is null)
        {
            return false;
        }

        if (request.Archived)
        {
            conversation.Archive(request.UserId);
        }
        else
        {
            conversation.Unarchive(request.UserId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
