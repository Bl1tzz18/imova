using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Publishers;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Auth.GoogleLogin;

public class GoogleLoginHandler(
    IGoogleTokenValidator googleTokenValidator,
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator jwtTokenGenerator,
    IExternalImageFetcher externalImageFetcher,
    IBlobStorageService blobStorageService,
    IApplicationDbContext dbContext,
    ILogger<GoogleLoginHandler> logger) : IRequestHandler<GoogleLoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var googleUser = await googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);
        if (googleUser is null)
        {
            throw new AuthenticationFailedException("Invalid Google token.");
        }

        var user = await userManager.FindByEmailAsync(googleUser.Email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = googleUser.Email,
                Email = googleUser.Email,
                EmailConfirmed = true,
                DisplayName = googleUser.Name,
            };

            // No password set — this account can only sign in via Google, never via the
            // email/password flow (LoginHandler's CheckPasswordAsync fails with no hash set).
            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                throw new ValidationException(result.Errors.Select(e =>
                    new FluentValidation.Results.ValidationFailure(nameof(GoogleLoginCommand.IdToken), e.Description)));
            }

            await userManager.AddToRoleAsync(user, Roles.User);

            // Every account publishes as an Individual by default — see PublisherProvisioning.
            dbContext.Publishers.Add(PublisherProvisioning.NewIndividualFor(user));
            await dbContext.SaveChangesAsync(cancellationToken);

            // New account only — a returning user who has since removed or replaced their
            // picture should never have it silently re-synced from Google on a later login.
            if (!string.IsNullOrWhiteSpace(googleUser.Picture))
            {
                await TrySyncProfilePictureAsync(user, googleUser.Picture, cancellationToken);
            }
        }
        else if (!user.EmailConfirmed)
        {
            // Google just re-verified this email, even though the account may have originally
            // been created via email/password registration (which leaves EmailConfirmed false —
            // we don't have email verification there yet). Without this, an account that started
            // as email/password and later signed in with Google would stay stuck as unconfirmed.
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var token = jwtTokenGenerator.GenerateToken(user, roles);

        return new AuthResultDto(
            token.Value,
            token.ExpiresAt,
            new AuthUserDto(
                user.Id,
                user.Email!,
                user.DisplayName,
                roles,
                string.IsNullOrWhiteSpace(user.PhoneNumber),
                user.ProfilePictureUrl));
    }

    // Never throws — a failure here (Google's URL is unreachable/expired, an unrecognized image
    // format, storage hiccup, ...) must not block account creation. Worst case, the user just
    // ends up with no picture, same as an email/password signup.
    private async Task TrySyncProfilePictureAsync(ApplicationUser user, string pictureUrl, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await externalImageFetcher.TryDownloadAsync(pictureUrl, cancellationToken);
            if (bytes is null)
            {
                logger.LogWarning("Could not download Google profile picture for new user {UserId}.", user.Id);
                return;
            }

            var contentType = ImageSignature.DetectContentType(bytes);
            var extension = contentType is null ? null : ImageSignature.ExtensionForContentType(contentType);
            if (contentType is null || extension is null)
            {
                logger.LogWarning(
                    "Google profile picture for new user {UserId} was not a recognized image format.", user.Id);
                return;
            }

            var blobName = blobStorageService.GenerateProfilePictureBlobName(user.Id, extension);
            await blobStorageService.UploadAsync(blobName, new MemoryStream(bytes), contentType, cancellationToken);

            user.ProfilePictureUrl = blobStorageService.GetPublicUrl(blobName);
            await userManager.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to sync Google profile picture for new user {UserId}.", user.Id);
        }
    }
}
