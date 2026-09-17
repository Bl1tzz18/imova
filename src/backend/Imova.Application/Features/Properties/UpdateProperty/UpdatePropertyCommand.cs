using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.UpdateProperty;

// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body (see CreatePropertyCommand's OwnerId for the same rule).
public record UpdatePropertyCommand(
    Guid Id,
    Guid RequestingUserId,
    bool IsAdmin,
    string Title,
    string Description,
    decimal Price) : IRequest<PropertyDto?>;
