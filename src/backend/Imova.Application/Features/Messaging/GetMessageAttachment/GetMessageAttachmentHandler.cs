using Imova.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Messaging.GetMessageAttachment;

public class GetMessageAttachmentHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMessageAttachmentQuery, MessageAttachmentFile?>
{
    public async Task<MessageAttachmentFile?> Handle(GetMessageAttachmentQuery request, CancellationToken cancellationToken)
    {
        var found = await (
                from m in dbContext.Messages.AsNoTracking()
                from a in m.Attachments
                join c in dbContext.Conversations.AsNoTracking() on m.ConversationId equals c.Id
                where a.Id == request.AttachmentId
                select new { a.BlobName, a.ContentType, c.InitiatorUserId, c.PublisherUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (found is null)
        {
            return null;
        }

        var allowed = request.IsAdmin || request.UserId == found.InitiatorUserId || request.UserId == found.PublisherUserId;
        return allowed ? new MessageAttachmentFile(found.BlobName, found.ContentType) : null;
    }
}
