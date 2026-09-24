using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Publishers;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.UpdateProfile;

public class UpdateProfileHandler(UserManager<ApplicationUser> userManager, IApplicationDbContext dbContext)
    : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.");

        user.DisplayName = request.DisplayName;
        user.PhoneNumber = request.PhoneNumber;
        await userManager.UpdateAsync(user);
        await PublisherProvisioning.SyncIndividualAsync(dbContext, user, cancellationToken);

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var hasPassword = await userManager.HasPasswordAsync(user);

        return new UserProfileDto(
            user.Id, user.Email!, user.DisplayName, user.PhoneNumber, user.ProfilePictureUrl, roles, hasPassword);
    }
}
