using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.Register;

// Language = the site's language at sign-up (ro/en/ru) — what the account's emails are written in.
// Optional: missing or unknown means Romanian. AcceptedTermsVersion = the version of the Terms of use
// + Privacy policy the person ticked; it must be the current one (LegalTerms.CurrentVersion).
public record RegisterCommand(
    string Email, string Password, string? DisplayName, string PhoneNumber, string? Language = null, string? AcceptedTermsVersion = null)
    : IRequest<AuthResultDto>;
