using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using Imova.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Messaging;

// The send path shared by StartConversation and SendMessage: checks the sender may write to the
// other participant, verifies attachments, flags suspicious content, saves, then (best-effort)
// pushes the message live and emails the recipient when appropriate.
public class MessageDelivery(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    IRealtimeNotifier realtimeNotifier,
    IPresenceTracker presenceTracker,
    IEmailSender emailSender,
    MessagingOptions options,
    TimeProvider timeProvider,
    ILogger<MessageDelivery> logger)
{
    public async Task<MessageDto> SendAsync(
        Conversation conversation,
        Guid senderUserId,
        string? body,
        IReadOnlyList<string>? attachmentBlobNames,
        CancellationToken cancellationToken)
    {
        var recipientUserId = conversation.OtherParticipant(senderUserId);
        await MessagingAccess.EnsureCanSendAsync(dbContext, senderUserId, recipientUserId, cancellationToken);

        var attachments = await MessageAttachments.ResolveAsync(blobStorageService, senderUserId, attachmentBlobNames, cancellationToken);
        var flagReason = MessageContentFilter.Check(body);
        var now = timeProvider.GetUtcNow();

        // "Unread" from the recipient's side, before this message lands.
        var unreadBefore = await dbContext.Messages.CountAsync(
            m => m.ConversationId == conversation.Id && m.SenderUserId != recipientUserId && m.ReadAt == null,
            cancellationToken);

        var message = conversation.AddMessage(senderUserId, body ?? string.Empty, attachments, flagReason is not null, flagReason, now);
        dbContext.Messages.Add(message);

        // No email while they're on the site — the live message and header badge already tell them.
        var sendEmail = !presenceTracker.IsOnline(recipientUserId) && conversation.ShouldEmail(recipientUserId, unreadBefore, now);
        if (sendEmail)
        {
            conversation.RecordEmailSent(recipientUserId, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = message.ToDto(blobStorageService);
        await BestEffortAsync(async () =>
        {
            await realtimeNotifier.MessageCreatedAsync(recipientUserId, senderUserId, dto, cancellationToken);
            var unread = await MessagingAccess.UnreadCountAsync(dbContext, recipientUserId, cancellationToken);
            await realtimeNotifier.UnreadCountChangedAsync(recipientUserId, unread, cancellationToken);
        }, "push a new message");

        if (sendEmail)
        {
            await BestEffortAsync(() => EmailAsync(conversation, senderUserId, recipientUserId, message, cancellationToken), "email a new-message notification");
        }

        return dto;
    }

    private async Task EmailAsync(
        Conversation conversation, Guid senderUserId, Guid recipientUserId, Message message, CancellationToken cancellationToken)
    {
        var people = await dbContext.Users.AsNoTracking()
            .Where(u => u.Id == senderUserId || u.Id == recipientUserId)
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToListAsync(cancellationToken);
        var recipientEmail = people.FirstOrDefault(p => p.Id == recipientUserId)?.Email;
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return;
        }

        var senderName = people.FirstOrDefault(p => p.Id == senderUserId)?.DisplayName ?? "Cineva";
        var title = await dbContext.Listings.AsNoTracking()
            .Where(l => l.Id == conversation.ListingId)
            .Select(l => l.Title)
            .FirstOrDefaultAsync(cancellationToken) ?? "un anunț";
        var preview = message.Body.Length > 300 ? message.Body[..300] + "…" : message.Body;
        var link = $"{options.WebBaseUrl.TrimEnd('/')}/messages/{conversation.Id}";

        await emailSender.SendAsync(
            new EmailMessage(
                recipientEmail,
                $"Mesaj nou pe IMOVA despre „{title}”",
                $"{senderName} ți-a trimis un mesaj despre anunțul „{title}”:\n\n" +
                $"{(preview.Length > 0 ? preview : "[imagine]")}\n\n" +
                $"Citește și răspunde: {link}\n"),
            cancellationToken);
    }

    // Saving the message is what matters; a failed push or email is logged, never surfaced.
    private async Task BestEffortAsync(Func<Task> action, string what)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not {What}.", what);
        }
    }
}
