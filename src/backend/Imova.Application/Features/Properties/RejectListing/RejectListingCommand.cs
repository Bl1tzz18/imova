using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.RejectListing;

// Admin-only (enforced by RejectListingHandler). IsAdmin is always supplied by the endpoint from
// the caller's JWT claims, never trusted from the request body; Reason comes from the request
// body (it's admin-authored feedback, not derivable from claims).
public record RejectListingCommand(Guid Id, bool IsAdmin, string Reason) : IRequest<PropertyDto?>;
