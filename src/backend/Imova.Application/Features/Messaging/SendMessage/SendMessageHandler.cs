using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.SendMessage;

public class SendMessageHandler(IApplicationDbContext dbContext, MessageDelivery messageDelivery)
    : IRequestHandler<SendMessageCommand, MessageDto?>
{
    public async Task<MessageDto?> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await MessagingAccess.FindForParticipantAsync(dbContext, request.ConversationId, request.UserId, cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        return await messageDelivery.SendAsync(
            conversation, request.UserId, request.Body, request.AttachmentBlobNames, cancellationToken);
    }
}
