using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Users.UploadProfilePicture;

// Unlike listing photos (RequestUploadUrl -> browser PUTs straight to blob storage via a SAS URL
// -> ConfirmMediaUpload), a profile picture is a single small file uploaded through the API
// server itself — simpler, and there's no gallery of these to justify the two-step SAS dance.
// Still reuses the same building blocks: ImageSignature for magic-byte sniffing and
// IBlobStorageService for the actual storage operations.
public class UploadProfilePictureHandler(UserManager<ApplicationUser> userManager, IBlobStorageService blobStorageService)
    : IRequestHandler<UploadProfilePictureCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UploadProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.");

        var contentType = ImageSignature.DetectContentType(request.Content);
        var extension = contentType is null ? null : ImageSignature.ExtensionForContentType(contentType);

        if (contentType is null || extension is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(UploadProfilePictureCommand.Content),
                    "The uploaded file is not a recognized image format (JPEG, PNG, WebP, GIF, BMP, HEIC, HEIF)."),
            ]);
        }

        // ProfilePictureUrl, whenever set, is always one of our own blobs (never an external URL
        // — see ApplicationUser.ProfilePictureUrl) — so if one exists, we own it and it's safe to
        // delete once the new one is safely in place.
        var previousBlobName = user.ProfilePictureUrl is not null
            ? blobStorageService.TryGetBlobNameFromUrl(user.ProfilePictureUrl)
            : null;

        var blobName = blobStorageService.GenerateProfilePictureBlobName(user.Id, extension);
        await blobStorageService.UploadAsync(blobName, new MemoryStream(request.Content), contentType, cancellationToken);

        user.ProfilePictureUrl = blobStorageService.GetPublicUrl(blobName);
        await userManager.UpdateAsync(user);

        if (previousBlobName is not null)
        {
            await blobStorageService.DeleteAsync(previousBlobName, cancellationToken);
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var hasPassword = await userManager.HasPasswordAsync(user);

        return new UserProfileDto(
            user.Id, user.Email!, user.DisplayName, user.PhoneNumber, user.ProfilePictureUrl, roles, hasPassword);
    }
}
