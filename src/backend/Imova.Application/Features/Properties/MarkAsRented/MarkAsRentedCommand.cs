using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.MarkAsRented;

// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body.
public record MarkAsRentedCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<PropertyDto?>;
