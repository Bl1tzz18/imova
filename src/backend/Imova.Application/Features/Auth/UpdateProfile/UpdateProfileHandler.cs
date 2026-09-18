using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.UpdateProfile;

public class UpdateProfileHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.");

        user.DisplayName = request.DisplayName;
        user.PhoneNumber = request.PhoneNumber;
        await userManager.UpdateAsync(user);

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var hasPassword = await userManager.HasPasswordAsync(user);

        return new UserProfileDto(user.Id, user.Email!, user.DisplayName, user.PhoneNumber, roles, hasPassword);
    }
}
