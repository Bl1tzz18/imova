using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.SendMessage;

// Null = no such conversation, or the caller isn't in it.
public record SendMessageCommand(Guid UserId, Guid ConversationId, string? Body, IReadOnlyList<string>? AttachmentBlobNames)
    : IRequest<MessageDto?>;
