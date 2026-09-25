using MediatR;

namespace Imova.Application.Features.Messaging.MarkMessagesDelivered;

// The caller's device has their messages now (they connected, or loaded the inbox/a thread):
// everything sent to them that was still only Sent becomes Delivered. Returns how many changed.
public record MarkMessagesDeliveredCommand(Guid UserId) : IRequest<int>;
