using MediatR;

namespace Imova.Application.Features.Account.RequestAccountDeletionLink;

// Emails the signed-in user a link to confirm deleting their account — how an account without a
// password (Google sign-up) proves it's really them. Any account may use it.
public record RequestAccountDeletionLinkCommand(Guid UserId) : IRequest;
