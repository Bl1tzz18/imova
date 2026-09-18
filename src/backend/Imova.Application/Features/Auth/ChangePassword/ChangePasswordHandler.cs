using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.ChangePassword;

public class ChangePasswordHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ChangePasswordCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.");

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
                    new ValidationFailure(nameof(ChangePasswordCommand.CurrentPassword), "Current password is required."),
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
            throw new ValidationException(result.Errors.Select(e =>
                new ValidationFailure(nameof(ChangePasswordCommand.NewPassword), e.Description)));
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        return new UserProfileDto(
            user.Id, user.Email!, user.DisplayName, user.PhoneNumber, user.ProfilePictureUrl, roles, HasPassword: true);
    }
}
