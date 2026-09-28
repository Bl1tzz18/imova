using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.ResendConfirmation;

public class ResendConfirmationHandler(
    UserManager<ApplicationUser> userManager,
    AccountEmails accountEmails,
    AuthEmailThrottle throttle) : IRequestHandler<ResendConfirmationCommand>
{
    public const string TooSoon = "A confirmation email was just sent. Wait a minute before asking for another one.";

    public async Task Handle(ResendConfirmationCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.");

        if (user.EmailConfirmed)
        {
            return;
        }

        if (!throttle.TryAcquire($"email-confirmation:{user.Id}"))
        {
            throw new TooManyRequestsException(TooSoon);
        }

        // Unlike at registration, a send failure here is the whole point of the request: let it surface.
        await accountEmails.SendEmailConfirmationAsync(user, cancellationToken);
    }
}
