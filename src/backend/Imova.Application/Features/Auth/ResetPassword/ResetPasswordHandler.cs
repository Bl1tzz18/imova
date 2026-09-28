using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Listings;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.ResetPassword;

public class ResetPasswordHandler(UserManager<ApplicationUser> userManager, IApplicationDbContext dbContext)
    : IRequestHandler<ResetPasswordCommand>
{
    public const string InvalidLink = "This link is invalid or has expired. Request a new one.";

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // An unknown email gets the same answer as a bad token — nothing to learn about accounts here.
        var user = await userManager.FindByEmailAsync(request.Email);
        var token = AccountTokens.Decode(request.Token);
        if (user is null || token is null)
        {
            throw InvalidLinkError();
        }

        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
            {
                throw InvalidLinkError();
            }

            throw new ValidationException(IdentityFailures.From(
                result.Errors, emailField: nameof(ResetPasswordCommand.Email), passwordField: nameof(ResetPasswordCommand.NewPassword)));
        }

        // A reset is also the way out of a sign-in lockout (see LoginHandler).
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);

        // Opening a link sent to the address proves the user owns it, same as the confirmation link.
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
            await ReviewEligibility.SubmitWaitingDraftsAsync(dbContext, user.Id, cancellationToken);
        }
    }

    private static ValidationException InvalidLinkError() =>
        new([CodedFailure.Of(nameof(ResetPasswordCommand.Token), InvalidLink, ErrorCodes.InvalidLink)]);
}
