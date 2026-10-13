using Imova.Application.Common;
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
    AppOptions appOptions,
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

        var dto = message.ToDto();
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
            .Select(u => new { u.Id, u.DisplayName, u.Email, u.PreferredLanguage })
            .ToListAsync(cancellationToken);
        var recipient = people.FirstOrDefault(p => p.Id == recipientUserId);
        if (string.IsNullOrWhiteSpace(recipient?.Email))
        {
            return;
        }

        var senderName = people.FirstOrDefault(p => p.Id == senderUserId)?.DisplayName;
        var title = await dbContext.Listings.AsNoTracking()
            .Where(l => l.Id == conversation.ListingId)
            .Select(l => l.Title)
            .FirstOrDefaultAsync(cancellationToken);
        var preview = message.Body.Length > 300 ? message.Body[..300] + "…" : message.Body;
        var link = appOptions.WebUrl($"/messages/{conversation.Id}");

        await emailSender.SendAsync(
            NewMessageEmail(recipient.Email, senderName, title, preview, link, recipient.PreferredLanguage), cancellationToken);
    }

    // Plain text, in the recipient's language. A missing sender name or listing title (both gone in
    // the meantime) reads as "someone" / "a listing"; an images-only message as "[image]".
    public static EmailMessage NewMessageEmail(
        string to, string? senderName, string? listingTitle, string preview, string link, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        // Russian says "от {sender}", so its fallback is in the genitive.
        var sender = senderName ?? T("Cineva", "Someone", "пользователя");
        var body = preview.Length > 0 ? preview : T("[imagine]", "[image]", "[изображение]");
        var (subject, intro) = listingTitle is { } title
            ? (T($"Mesaj nou pe IMOVA despre „{title}”", $"New message on IMOVA about “{title}”", $"Новое сообщение на IMOVA по объявлению «{title}»"),
               T($"{sender} ți-a trimis un mesaj despre anunțul „{title}”:",
                   $"{sender} sent you a message about the listing “{title}”:",
                   $"Новое сообщение от {sender} по объявлению «{title}»:"))
            : (T("Mesaj nou pe IMOVA despre un anunț", "New message on IMOVA about a listing", "Новое сообщение на IMOVA по объявлению"),
               T($"{sender} ți-a trimis un mesaj despre un anunț:",
                   $"{sender} sent you a message about a listing:",
                   $"Новое сообщение от {sender} по объявлению:"));

        return new EmailMessage(
            to, subject, $"{intro}\n\n{body}\n\n{T("Citește și răspunde", "Read and reply", "Прочитать и ответить")}: {link}\n");
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
