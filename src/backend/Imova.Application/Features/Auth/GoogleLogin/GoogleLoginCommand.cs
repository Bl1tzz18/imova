using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.GoogleLogin;

// Language = the site's language at that moment; only used when this sign-in creates the account
// (see RegisterCommand). An existing account keeps its own.
public record GoogleLoginCommand(string IdToken, string? Language = null) : IRequest<AuthResultDto>;
