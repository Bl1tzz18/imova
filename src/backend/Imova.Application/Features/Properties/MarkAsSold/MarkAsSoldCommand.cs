using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.MarkAsSold;

// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body.
public record MarkAsSoldCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<PropertyDto?>;
