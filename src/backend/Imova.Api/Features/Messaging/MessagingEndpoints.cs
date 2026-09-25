using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Messaging.Admin;
using Imova.Application.Features.Messaging.GetConversationIdForListing;
using Imova.Application.Features.Messaging.GetConversations;
using Imova.Application.Features.Messaging.GetConversationThread;
using Imova.Application.Features.Messaging.GetRealtimeToken;
using Imova.Application.Features.Messaging.GetUnreadCount;
using Imova.Application.Features.Messaging.MarkConversationRead;
using Imova.Application.Features.Messaging.MarkMessagesDelivered;
using Imova.Application.Features.Messaging.ReportConversation;
using Imova.Application.Features.Messaging.RequestAttachmentUploadUrl;
using Imova.Application.Features.Messaging.SendMessage;
using Imova.Application.Features.Messaging.SetConversationArchived;
using Imova.Application.Features.Messaging.SetUserBlocked;
using Imova.Application.Features.Messaging.StartConversation;
using Imova.Domain.Messaging;
using MediatR;

namespace Imova.Api.Features.Messaging;

public record StartConversationRequest(Guid ListingId, string? Body, IReadOnlyList<string>? AttachmentBlobNames);

public record SendMessageRequest(string? Body, IReadOnlyList<string>? AttachmentBlobNames);

public record ReportConversationRequest(ReportReason Reason, string? Details);

public record AttachmentUploadUrlRequest(string FileExtension);

public static class MessagingEndpoints
{
    public static void MapMessagingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/messaging").RequireAuthorization();

        // Loading the inbox or a thread means the caller's device has their messages: anything
        // still only "Sent" to them becomes Delivered first.
        group.MapGet("/conversations", async (ClaimsPrincipal user, ISender sender, CancellationToken ct, string? search, bool archived = false) =>
        {
            var userId = user.GetUserId();
            await sender.Send(new MarkMessagesDeliveredCommand(userId), ct);
            return Results.Ok(await sender.Send(new GetConversationsQuery(userId, search, archived), ct));
        });

        group.MapGet("/conversations/{id:guid}", async (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct, Guid? before, int pageSize = 30) =>
        {
            var userId = user.GetUserId();
            await sender.Send(new MarkMessagesDeliveredCommand(userId), ct);
            var thread = await sender.Send(new GetConversationThreadQuery(userId, id, before, pageSize), ct);
            return thread is null ? Results.NotFound() : Results.Ok(thread);
        });

        group.MapGet("/conversations/by-listing/{listingId:guid}", async (Guid listingId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(new GetConversationIdForListingQuery(user.GetUserId(), listingId), ct);
            return id is null ? Results.NotFound() : Results.Ok(new { conversationId = id });
        });

        group.MapPost("/conversations", async (StartConversationRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new StartConversationCommand(user.GetUserId(), request.ListingId, request.Body, request.AttachmentBlobNames), ct);
            return result is null
                ? Results.NotFound()
                : result.Reused
                    ? Results.Ok(result)
                    : Results.Created($"/api/v1/messaging/conversations/{result.ConversationId}", result);
        });

        group.MapPost("/conversations/{id:guid}/messages", async (Guid id, SendMessageRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var message = await sender.Send(new SendMessageCommand(user.GetUserId(), id, request.Body, request.AttachmentBlobNames), ct);
            return message is null ? Results.NotFound() : Results.Created($"/api/v1/messaging/conversations/{id}", message);
        });

        group.MapPost("/conversations/{id:guid}/read", async (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            await sender.Send(new MarkConversationReadCommand(user.GetUserId(), id), ct) ? Results.NoContent() : Results.NotFound());

        group.MapPost("/conversations/{id:guid}/archive", (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            SetArchived(id, true, user, sender, ct));
        group.MapPost("/conversations/{id:guid}/unarchive", (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            SetArchived(id, false, user, sender, ct));

        group.MapPost("/conversations/{id:guid}/block", (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            SetBlocked(id, true, user, sender, ct));
        group.MapPost("/conversations/{id:guid}/unblock", (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            SetBlocked(id, false, user, sender, ct));

        group.MapPost("/conversations/{id:guid}/report", async (Guid id, ReportConversationRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            await sender.Send(new ReportConversationCommand(user.GetUserId(), id, request.Reason, request.Details), ct)
                ? Results.NoContent()
                : Results.NotFound());

        group.MapGet("/unread-count", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetUnreadCountQuery(user.GetUserId()), ct)));

        group.MapPost("/attachments/upload-url", async (AttachmentUploadUrlRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new RequestAttachmentUploadUrlCommand(user.GetUserId(), request.FileExtension), ct)));

        // Called server-side by the web app (which holds the session token) and handed to the
        // browser for the hub connection.
        group.MapPost("/realtime-token", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetRealtimeTokenQuery(user.GetUserId()), ct)));

        MapAdmin(app.MapGroup("/api/v1/admin/messaging").RequireAuthorization());
    }

    private static void MapAdmin(RouteGroupBuilder admin)
    {
        admin.MapGet("/reports", async (ClaimsPrincipal user, ISender sender, CancellationToken ct, bool includeResolved = false) =>
            Results.Ok(await sender.Send(new GetMessagingReportsQuery(user.IsInRole(Roles.Admin), includeResolved), ct)));

        admin.MapGet("/flagged-messages", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetFlaggedMessagesQuery(user.IsInRole(Roles.Admin)), ct)));

        admin.MapGet("/conversations/{id:guid}", async (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var conversation = await sender.Send(new GetConversationForAdminQuery(user.IsInRole(Roles.Admin), id), ct);
            return conversation is null ? Results.NotFound() : Results.Ok(conversation);
        });

        admin.MapPost("/reports/{id:guid}/resolve", async (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            await sender.Send(new ResolveMessagingReportCommand(user.IsInRole(Roles.Admin), user.GetUserId(), id), ct)
                ? Results.NoContent()
                : Results.NotFound());

        admin.MapPost("/users/{userId:guid}/ban", (Guid userId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            SetBan(userId, true, user, sender, ct));
        admin.MapPost("/users/{userId:guid}/unban", (Guid userId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            SetBan(userId, false, user, sender, ct));
    }

    private static async Task<IResult> SetArchived(Guid id, bool archived, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        await sender.Send(new SetConversationArchivedCommand(user.GetUserId(), id, archived), ct) ? Results.NoContent() : Results.NotFound();

    private static async Task<IResult> SetBlocked(Guid id, bool blocked, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        await sender.Send(new SetUserBlockedCommand(user.GetUserId(), id, blocked), ct) ? Results.NoContent() : Results.NotFound();

    private static async Task<IResult> SetBan(Guid userId, bool banned, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        await sender.Send(new SetMessagingBanCommand(user.IsInRole(Roles.Admin), userId, banned), ct) ? Results.NoContent() : Results.NotFound();
}
