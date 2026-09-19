using MediatR;

namespace Imova.Application.Features.Media.DeleteMedia;

// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body (see DeletePropertyCommand for the same rule).
public record DeleteMediaCommand(Guid PropertyId, Guid MediaId, Guid RequestingUserId, bool IsAdmin) : IRequest<bool>;
