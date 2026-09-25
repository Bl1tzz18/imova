using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.StartConversation;

// "Scrie mesaj" on a listing: opens (or reuses) the caller's conversation with the listing's
// publisher and sends the first message. Null = the listing doesn't exist or isn't Active.
public record StartConversationCommand(Guid UserId, Guid ListingId, string? Body, IReadOnlyList<string>? AttachmentBlobNames)
    : IRequest<StartConversationResultDto?>;
