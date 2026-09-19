using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.ArchiveProperty;

// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body.
public record ArchivePropertyCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<PropertyDto?>;
