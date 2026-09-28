using MediatR;

namespace Imova.Application.Features.Auth.Logout;

// Signing out ends the session on the server too: its refresh token can't be used any more. The
// login token itself still works until it runs out (at most Jwt:ExpiryMinutes).
public record LogoutCommand(string RefreshToken) : IRequest;
