using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.ReinstateListing;

// Admin-only (enforced by ReinstateListingHandler). IsAdmin is always supplied by the endpoint
// from the caller's JWT claims, never trusted from the request body.
public record ReinstateListingCommand(Guid Id, bool IsAdmin) : IRequest<PropertyDto?>;
