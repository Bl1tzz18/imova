using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.SuspendListing;

// Admin-only (enforced by SuspendListingHandler). IsAdmin is always supplied by the endpoint from
// the caller's JWT claims, never trusted from the request body.
public record SuspendListingCommand(Guid Id, bool IsAdmin, string Reason) : IRequest<PropertyDto?>;
