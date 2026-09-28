using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Auth.Sessions;
using Imova.Application.Common.Validation;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.ChangePassword;

public class ChangePasswordHandler(UserManager<ApplicationUser> userManager, AuthSessions authSessions)
    : IRequestHandler<ChangePasswordCommand, ChangePasswordResultDto>
{
    public async Task<ChangePasswordResultDto> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.", ErrorCodes.UserNotFound);

        var hasPassword = await userManager.HasPasswordAsync(user);

        // Google-only accounts (created via GoogleLoginHandler) have no password hash yet — for
        // those, "change password" really means "set a password for the first time", which
        // Identity models as AddPasswordAsync rather than ChangePasswordAsync (there's nothing to
        // check the current password against).
        IdentityResult result;
        if (hasPassword)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword))
            {
                throw new ValidationException(
                [
                    CodedFailure.Of(nameof(ChangePasswordCommand.CurrentPassword), "Current password is required.", ErrorCodes.CurrentPasswordRequired),
                ]);
            }

            result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        }
        else
        {
            result = await userManager.AddPasswordAsync(user, request.NewPassword);
        }

        if (!result.Succeeded)
        {
            throw new ValidationException(IdentityFailures.From(
                result.Errors,
                emailField: nameof(ChangePasswordCommand.NewPassword),
                passwordField: nameof(ChangePasswordCommand.NewPassword),
                currentPasswordField: nameof(ChangePasswordCommand.CurrentPassword)));
        }

        // Identity has already changed the security stamp: every session is signed out, and this one
        // continues with tokens made from the new stamp.
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var session = await authSessions.ContinueAsync(user, request.SessionId, cancellationToken);
        return new ChangePasswordResultDto(
            new UserProfileDto(
                user.Id, user.Email!, user.DisplayName, user.PhoneNumber, user.ProfilePictureUrl, roles, HasPassword: true, user.EmailConfirmed),
            session);
    }
}
