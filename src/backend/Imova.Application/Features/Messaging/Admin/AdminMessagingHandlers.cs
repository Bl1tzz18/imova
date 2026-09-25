using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using Imova.Domain.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Messaging.Admin;

internal static class AdminMessaging
{
    public static void EnsureAdmin(bool isAdmin)
    {
        if (!isAdmin)
        {
            throw new ForbiddenAccessException();
        }
    }

    public static async Task<Dictionary<Guid, MessagingUserDto>> UsersAsync(
        IApplicationDbContext dbContext, IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        return await dbContext.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new MessagingUserDto(u.Id, u.DisplayName, u.Email, u.IsBannedFromMessaging))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
    }

    public static MessagingUserDto UserOrPlaceholder(Dictionary<Guid, MessagingUserDto> users, Guid id) =>
        users.GetValueOrDefault(id) ?? new MessagingUserDto(id, null, null, false);

    public static MessagingUserDto? OptionalUser(Dictionary<Guid, MessagingUserDto> users, Guid? id) =>
        id is { } value ? UserOrPlaceholder(users, value) : null;

    public static async Task<Dictionary<Guid, ConversationListingDto>> ListingsAsync(
        IApplicationDbContext dbContext, IEnumerable<Guid> listingIds, CancellationToken cancellationToken)
    {
        var ids = listingIds.Distinct().ToList();
        return await dbContext.Listings.AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .Select(l => new ConversationListingDto(l.Id, l.Title, null))
            .ToDictionaryAsync(l => l.Id, cancellationToken);
    }
}

public class GetMessagingReportsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMessagingReportsQuery, List<MessagingReportDto>>
{
    public async Task<List<MessagingReportDto>> Handle(GetMessagingReportsQuery request, CancellationToken cancellationToken)
    {
        AdminMessaging.EnsureAdmin(request.IsAdmin);

        var query =
            from r in dbContext.ConversationReports.AsNoTracking()
            join c in dbContext.Conversations.AsNoTracking() on r.ConversationId equals c.Id
            where (r.ResolvedAt != null) == request.Resolved
            select new { Report = r, Conversation = c };
        var rows = await (request.Resolved
                ? query.OrderByDescending(x => x.Report.ResolvedAt)
                : query.OrderByDescending(x => x.Report.CreatedAt))
            .Take(200)
            .ToListAsync(cancellationToken);

        var users = await AdminMessaging.UsersAsync(
            dbContext,
            rows.SelectMany(x => new[] { x.Conversation.InitiatorUserId, x.Conversation.PublisherUserId })
                .Concat(rows.Where(x => x.Report.ResolvedByUserId != null).Select(x => x.Report.ResolvedByUserId!.Value)),
            cancellationToken);
        var listings = await AdminMessaging.ListingsAsync(dbContext, rows.Select(x => x.Conversation.ListingId), cancellationToken);

        return rows.Select(x => new MessagingReportDto(
                x.Report.Id,
                x.Conversation.Id,
                x.Report.Reason.ToString(),
                x.Report.Details,
                x.Report.CreatedAt,
                x.Report.ResolvedAt,
                AdminMessaging.UserOrPlaceholder(users, x.Report.ReporterUserId),
                AdminMessaging.UserOrPlaceholder(users, x.Conversation.OtherParticipant(x.Report.ReporterUserId)),
                listings.GetValueOrDefault(x.Conversation.ListingId) ?? new ConversationListingDto(x.Conversation.ListingId, null, null),
                AdminMessaging.OptionalUser(users, x.Report.ResolvedByUserId)))
            .ToList();
    }
}

public class GetFlaggedMessagesHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetFlaggedMessagesQuery, List<FlaggedMessageDto>>
{
    public async Task<List<FlaggedMessageDto>> Handle(GetFlaggedMessagesQuery request, CancellationToken cancellationToken)
    {
        AdminMessaging.EnsureAdmin(request.IsAdmin);

        var flagged = dbContext.Messages.AsNoTracking()
            .Include(m => m.Attachments)
            .Where(m => m.IsFlagged && (m.FlagResolvedAt != null) == request.Resolved);
        var messages = await (request.Resolved
                ? flagged.OrderByDescending(m => m.FlagResolvedAt)
                : flagged.OrderByDescending(m => m.CreatedAt))
            .Take(200)
            .ToListAsync(cancellationToken);
        var users = await AdminMessaging.UsersAsync(
            dbContext,
            messages.Select(m => m.SenderUserId)
                .Concat(messages.Where(m => m.FlagResolvedByUserId != null).Select(m => m.FlagResolvedByUserId!.Value)),
            cancellationToken);

        return messages
            .Select(m => new FlaggedMessageDto(
                m.ToDto(),
                m.FlagReason ?? string.Empty,
                AdminMessaging.UserOrPlaceholder(users, m.SenderUserId),
                m.FlagResolvedAt,
                AdminMessaging.OptionalUser(users, m.FlagResolvedByUserId)))
            .ToList();
    }
}

public class GetConversationForAdminHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetConversationForAdminQuery, AdminConversationDto?>
{
    public async Task<AdminConversationDto?> Handle(GetConversationForAdminQuery request, CancellationToken cancellationToken)
    {
        AdminMessaging.EnsureAdmin(request.IsAdmin);

        var conversation = await dbContext.Conversations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        var messages = await dbContext.Messages.AsNoTracking()
            .Include(m => m.Attachments)
            .Where(m => m.ConversationId == conversation.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
        var users = await AdminMessaging.UsersAsync(
            dbContext, [conversation.InitiatorUserId, conversation.PublisherUserId], cancellationToken);
        var listings = await AdminMessaging.ListingsAsync(dbContext, [conversation.ListingId], cancellationToken);

        return new AdminConversationDto(
            conversation.Id,
            listings.GetValueOrDefault(conversation.ListingId) ?? new ConversationListingDto(conversation.ListingId, null, null),
            AdminMessaging.UserOrPlaceholder(users, conversation.InitiatorUserId),
            AdminMessaging.UserOrPlaceholder(users, conversation.PublisherUserId),
            messages.Select(m => m.ToDto()).ToList());
    }
}

public class SetMessagingReportResolvedHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<SetMessagingReportResolvedCommand, bool>
{
    public async Task<bool> Handle(SetMessagingReportResolvedCommand request, CancellationToken cancellationToken)
    {
        AdminMessaging.EnsureAdmin(request.IsAdmin);

        var report = await dbContext.ConversationReports.FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);
        if (report is null)
        {
            return false;
        }

        if (request.Resolved)
        {
            report.Resolve(request.AdminUserId, timeProvider.GetUtcNow());
        }
        else
        {
            report.Reopen();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class SetFlaggedMessageResolvedHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<SetFlaggedMessageResolvedCommand, bool>
{
    public async Task<bool> Handle(SetFlaggedMessageResolvedCommand request, CancellationToken cancellationToken)
    {
        AdminMessaging.EnsureAdmin(request.IsAdmin);

        var message = await dbContext.Messages.FirstOrDefaultAsync(m => m.Id == request.MessageId && m.IsFlagged, cancellationToken);
        if (message is null)
        {
            return false;
        }

        if (request.Resolved)
        {
            message.ResolveFlag(request.AdminUserId, timeProvider.GetUtcNow());
        }
        else
        {
            message.ReopenFlag();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class SetMessagingBanHandler(IApplicationDbContext dbContext) : IRequestHandler<SetMessagingBanCommand, bool>
{
    public async Task<bool> Handle(SetMessagingBanCommand request, CancellationToken cancellationToken)
    {
        AdminMessaging.EnsureAdmin(request.IsAdmin);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.IsBannedFromMessaging = request.Banned;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
