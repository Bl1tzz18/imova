using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Users.RemoveProfilePicture;

public class RemoveProfilePictureHandler(UserManager<ApplicationUser> userManager, IBlobStorageService blobStorageService)
    : IRequestHandler<RemoveProfilePictureCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(RemoveProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.");

        // ProfilePictureUrl, whenever set, is always one of our own blobs (never an external URL
        // — see ApplicationUser.ProfilePictureUrl), so it's always safe to delete.
        if (user.ProfilePictureUrl is not null)
        {
            var blobName = blobStorageService.TryGetBlobNameFromUrl(user.ProfilePictureUrl);
            if (blobName is not null)
            {
                await blobStorageService.DeleteAsync(blobName, cancellationToken);
            }

            user.ProfilePictureUrl = null;
            await userManager.UpdateAsync(user);
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var hasPassword = await userManager.HasPasswordAsync(user);

        return new UserProfileDto(
            user.Id, user.Email!, user.DisplayName, user.PhoneNumber, user.ProfilePictureUrl, roles, hasPassword);
    }
}
