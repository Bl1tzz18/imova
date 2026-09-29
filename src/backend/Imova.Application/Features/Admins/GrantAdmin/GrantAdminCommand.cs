using MediatR;

namespace Imova.Application.Features.Admins.GrantAdmin;

// An admin making another existing account an admin. ActorUserId and IpAddress come from the
// request context (JWT, connection), never from the body; Password is the *acting admin's* own —
// a second proof it's really them, not someone at their unlocked laptop or holding a stolen token.
public record GrantAdminCommand(Guid ActorUserId, string Email, string? Password, string? IpAddress) : IRequest;
