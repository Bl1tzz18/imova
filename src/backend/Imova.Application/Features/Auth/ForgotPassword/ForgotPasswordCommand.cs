using MediatR;

namespace Imova.Application.Features.Auth.ForgotPassword;

// Emails a password-reset link. Deliberately says nothing about whether the account exists: the
// endpoint answers the same (204) either way, so it can't be used to find out who has an account.
public record ForgotPasswordCommand(string Email) : IRequest;
