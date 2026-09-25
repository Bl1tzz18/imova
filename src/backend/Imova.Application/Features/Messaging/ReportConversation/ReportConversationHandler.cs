using Imova.Application.Common.Interfaces;
using Imova.Domain.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.ReportConversation;

public class ReportConversationHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<ReportConversationCommand, bool>
{
    public async Task<bool> Handle(ReportConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await MessagingAccess.FindForParticipantAsync(dbContext, request.ConversationId, request.UserId, cancellationToken);
        if (conversation is null)
        {
            return false;
        }

        dbContext.ConversationReports.Add(
            ConversationReport.Create(conversation, request.UserId, request.Reason, request.Details, timeProvider.GetUtcNow()));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
