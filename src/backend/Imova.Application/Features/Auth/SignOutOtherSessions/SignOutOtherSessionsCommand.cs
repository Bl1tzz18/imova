using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.SignOutOtherSessions;

// "Sign out on every other device": a new security stamp revokes every existing login token (see
// SessionStamp); the caller gets a fresh one so the session that asked stays signed in.
// SessionId: the caller's "sid" claim — the session that stays.
public record SignOutOtherSessionsCommand(Guid UserId, Guid? SessionId = null) : IRequest<SessionTokenDto>;
