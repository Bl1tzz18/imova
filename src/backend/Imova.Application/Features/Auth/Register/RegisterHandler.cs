using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Publishers;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.Register;

public class RegisterHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator jwtTokenGenerator,
    IApplicationDbContext dbContext)
    : IRequestHandler<RegisterCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            PhoneNumber = request.PhoneNumber,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ValidationException(ToValidationFailures(result.Errors));
        }

        await userManager.AddToRoleAsync(user, Roles.User);

        // Every account publishes as an Individual by default — see PublisherProvisioning.
        dbContext.Publishers.Add(PublisherProvisioning.NewIndividualFor(user));
        await dbContext.SaveChangesAsync(cancellationToken);

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

    private static IEnumerable<ValidationFailure> ToValidationFailures(IEnumerable<IdentityError> errors) =>
        errors.Select(e => new ValidationFailure(nameof(RegisterCommand.Email), e.Description));
}
