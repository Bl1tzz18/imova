using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.GetProfile;

public class GetProfileHandler(UserManager<ApplicationUser> userManager) : IRequestHandler<GetProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.");

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var hasPassword = await userManager.HasPasswordAsync(user);

        return new UserProfileDto(user.Id, user.Email!, user.DisplayName, user.PhoneNumber, roles, hasPassword);
    }
}
