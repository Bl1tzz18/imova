using Imova.Application.Common.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Auth.ForgotPassword;

public class ForgotPasswordHandler(
    UserManager<ApplicationUser> userManager,
    AccountEmails accountEmails,
    AuthEmailThrottle throttle,
    ILogger<ForgotPasswordHandler> logger) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Every early return and failure below looks the same to the caller as a sent email — see
        // ForgotPasswordCommand. A Google-only account (no password yet) gets a link too: resetting
        // is how it sets its first password.
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !throttle.TryAcquire($"password-reset:{user.Id}"))
        {
            return;
        }

        try
        {
            await accountEmails.SendPasswordResetAsync(user, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not send a password reset link to user {UserId}.", user.Id);
        }
    }
}
