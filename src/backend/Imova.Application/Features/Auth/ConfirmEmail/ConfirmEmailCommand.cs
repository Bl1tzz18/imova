using MediatR;

namespace Imova.Application.Features.Auth.ConfirmEmail;

// From the emailed link (see AccountEmails.SendEmailConfirmationAsync). No sign-in needed: the
// link may well be opened on another device than the one the account was created on.
public record ConfirmEmailCommand(Guid UserId, string Token) : IRequest;
