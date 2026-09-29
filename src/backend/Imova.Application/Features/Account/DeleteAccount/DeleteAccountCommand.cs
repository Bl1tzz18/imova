using MediatR;

namespace Imova.Application.Features.Account.DeleteAccount;

// The signed-in user deleting their own account, confirming with their password. An account
// without a password (Google sign-up) confirms through an emailed link instead — see
// RequestAccountDeletionLink / ConfirmAccountDeletion.
public record DeleteAccountCommand(Guid UserId, string? Password) : IRequest;
