using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.ApproveListing;

// Admin-only (enforced by ApproveListingHandler) — no RequestingUserId, since the owner never
// approves their own listing. IsAdmin is always supplied by the endpoint from the caller's JWT
// claims, never trusted from the request body.
public record ApproveListingCommand(Guid Id, bool IsAdmin) : IRequest<PropertyDto?>;
