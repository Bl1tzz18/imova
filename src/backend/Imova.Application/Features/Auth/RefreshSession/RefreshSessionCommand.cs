using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.RefreshSession;

// Trades a refresh token for a new login token + refresh token (see AuthSessions). 401 with
// auth.sessionExpired when the session is over — the user signs in again.
public record RefreshSessionCommand(string RefreshToken) : IRequest<SessionTokenDto>;
