using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Account.RequestAccountDeletionLink;

public class RequestAccountDeletionLinkHandler(
    UserManager<ApplicationUser> userManager,
    AccountDeletionEmails emails,
    AuthEmailThrottle throttle) : IRequestHandler<RequestAccountDeletionLinkCommand>
{
    public const string TooSoon = "A link was just sent. Wait a minute before asking for another one.";

    public async Task Handle(RequestAccountDeletionLinkCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.", ErrorCodes.UserNotFound);

        if (!throttle.TryAcquire($"account-deletion:{user.Id}"))
        {
            throw new TooManyRequestsException(TooSoon, ErrorCodes.EmailThrottled);
        }

        await emails.SendConfirmationLinkAsync(user, cancellationToken);
    }
}
