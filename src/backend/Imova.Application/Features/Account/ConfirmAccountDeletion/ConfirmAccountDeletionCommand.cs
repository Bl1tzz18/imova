using MediatR;

namespace Imova.Application.Features.Account.ConfirmAccountDeletion;

// From the page the emailed link opens (see AccountDeletionEmails), after the user presses the
// final button there. No sign-in needed — the link may be opened on another device; the token
// proves the request came from the account's inbox. It stops working once the account's security
// stamp changes (a password change, "sign out other devices") and, naturally, once it's used.
public record ConfirmAccountDeletionCommand(Guid UserId, string Token) : IRequest;
