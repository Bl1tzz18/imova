using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.Login;

// RememberMe: "Ține-mă minte" — see AuthSessionOptions.
public record LoginCommand(string Email, string Password, bool RememberMe = false) : IRequest<AuthResultDto>;
