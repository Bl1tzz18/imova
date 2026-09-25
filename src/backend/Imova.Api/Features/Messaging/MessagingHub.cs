using Imova.Api.Common;
using Imova.Application.Features.Messaging.GetConversationCounterparts;
using Imova.Application.Features.Messaging.MarkMessagesDelivered;
using Imova.Contracts.Messaging;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Imova.Api.Features.Messaging;

// The realtime channel: messages themselves are sent over REST (and pushed from there — see
// RealtimeNotifier); the hub carries presence and typing, and marks pending messages Delivered as
// soon as their recipient connects. Every user joins their own group, so a push reaches all their tabs.
[Authorize(AuthenticationSchemes = RealtimeAuth.Scheme)]
public class MessagingHub(ISender sender, PresenceTracker presenceTracker) : Hub
{
    public const string Path = "/hubs/messaging";

    public static string UserGroup(Guid userId) => $"user:{userId}";

    private Guid UserId => Context.User!.GetUserId();

    public override async Task OnConnectedAsync()
    {
        var userId = UserId;
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        if (presenceTracker.Connect(userId, Context.ConnectionId))
        {
            await NotifyPresenceAsync(userId, online: true);
        }

        await sender.Send(new MarkMessagesDeliveredCommand(userId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = UserId;
        if (presenceTracker.Disconnect(userId, Context.ConnectionId))
        {
            await NotifyPresenceAsync(userId, online: false);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // "I'm typing in this conversation" — relayed to the other participant only; ignored when the
    // caller isn't in the conversation.
    public async Task Typing(Guid conversationId)
    {
        var userId = UserId;
        var others = await sender.Send(new GetConversationCounterpartsQuery(userId, conversationId));
        if (others.Count > 0)
        {
            await Clients.Groups(others.Select(UserGroup).ToList())
                .SendAsync(RealtimeEvents.Typing, new TypingDto(conversationId, userId));
        }
    }

    // Which of these users are online — limited to people the caller has a conversation with.
    public async Task<IReadOnlyList<Guid>> GetOnlineUsers(IReadOnlyList<Guid> userIds)
    {
        var counterparts = (await sender.Send(new GetConversationCounterpartsQuery(UserId))).ToHashSet();
        return userIds.Where(id => counterparts.Contains(id) && presenceTracker.IsOnline(id)).Distinct().ToList();
    }

    private async Task NotifyPresenceAsync(Guid userId, bool online)
    {
        var counterparts = await sender.Send(new GetConversationCounterpartsQuery(userId));
        if (counterparts.Count > 0)
        {
            await Clients.Groups(counterparts.Select(UserGroup).ToList())
                .SendAsync(RealtimeEvents.PresenceChanged, new PresenceDto(userId, online));
        }
    }
}
