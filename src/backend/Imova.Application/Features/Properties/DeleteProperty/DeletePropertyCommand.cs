using MediatR;

namespace Imova.Application.Features.Properties.DeleteProperty;

// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body.
public record DeletePropertyCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<bool>;
