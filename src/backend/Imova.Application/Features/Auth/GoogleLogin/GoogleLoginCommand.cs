using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.GoogleLogin;

// Language = the site's language at that moment; only used when this sign-in creates the account
// (see RegisterCommand). An existing account keeps its own. AcceptedTermsVersion likewise: a sign-in
// that would create an account needs the current Terms version (the notice by the Google button);
// an existing account signs in without it.
public record GoogleLoginCommand(string IdToken, string? Language = null, string? AcceptedTermsVersion = null) : IRequest<AuthResultDto>;
