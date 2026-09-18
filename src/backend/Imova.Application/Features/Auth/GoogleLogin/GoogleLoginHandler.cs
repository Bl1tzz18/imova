using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.GoogleLogin;

public class GoogleLoginHandler(
    IGoogleTokenValidator googleTokenValidator,
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<GoogleLoginCommand, AuthResultDto>
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
            new AuthUserDto(user.Id, user.Email!, user.DisplayName, roles, string.IsNullOrWhiteSpace(user.PhoneNumber)));
    }
}
