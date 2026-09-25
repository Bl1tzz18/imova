using MediatR;

namespace Imova.Application.Features.Messaging.GetConversationIdForListing;

// The caller's existing conversation about a listing (as the visitor), if any — lets "Scrie
// mesaj" open it directly instead of starting over.
public record GetConversationIdForListingQuery(Guid UserId, Guid ListingId) : IRequest<Guid?>;
