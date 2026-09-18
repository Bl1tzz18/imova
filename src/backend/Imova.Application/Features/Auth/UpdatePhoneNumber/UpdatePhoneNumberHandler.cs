using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.UpdatePhoneNumber;

public class UpdatePhoneNumberHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<UpdatePhoneNumberCommand, AuthUserDto>
{
    public async Task<AuthUserDto> Handle(UpdatePhoneNumberCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.");

        user.PhoneNumber = request.PhoneNumber;
        await userManager.UpdateAsync(user);

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        return new AuthUserDto(
            user.Id,
            user.Email!,
            user.DisplayName,
            roles,
            string.IsNullOrWhiteSpace(user.PhoneNumber),
            user.ProfilePictureUrl);
    }
}
