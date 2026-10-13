using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.Register;

// Language = the site's language at sign-up (ro/en/ru) — what the account's emails are written in.
// Optional: missing or unknown means Romanian.
public record RegisterCommand(string Email, string Password, string? DisplayName, string PhoneNumber, string? Language = null)
    : IRequest<AuthResultDto>;
