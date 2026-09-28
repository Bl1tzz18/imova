using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Publishers;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Auth.Register;

public class RegisterHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator jwtTokenGenerator,
    IApplicationDbContext dbContext,
    AccountEmails accountEmails,
    ILogger<RegisterHandler> logger)
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

        // Best effort: the account exists either way, and the user can ask for another link
        // (resend-confirmation) — a mail server hiccup must not fail the registration.
        try
        {
            await accountEmails.SendEmailConfirmationAsync(user, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not send the email confirmation link to new user {UserId}.", user.Id);
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
                user.ProfilePictureUrl,
                user.EmailConfirmed));
    }

    private static IEnumerable<ValidationFailure> ToValidationFailures(IEnumerable<IdentityError> errors) =>
        errors.Select(e => new ValidationFailure(nameof(RegisterCommand.Email), e.Description));
}
