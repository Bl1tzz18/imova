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

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var token = jwtTokenGenerator.GenerateToken(user, roles);

        return new AuthResultDto(token.Value, token.ExpiresAt, new AuthUserDto(user.Id, user.Email!, user.DisplayName, roles));
    }
}
