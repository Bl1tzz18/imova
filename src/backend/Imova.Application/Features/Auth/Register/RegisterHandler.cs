using FluentValidation;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Auth.Sessions;
using Imova.Application.Features.Publishers;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Auth.Register;

public class RegisterHandler(
    UserManager<ApplicationUser> userManager,
    AuthSessions authSessions,
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
            throw new ValidationException(IdentityFailures.From(
                result.Errors, emailField: nameof(RegisterCommand.Email), passwordField: nameof(RegisterCommand.Password)));
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

        var session = await authSessions.StartAsync(user, persistent: true, cancellationToken);
        return AuthResults.For(user, roles, session);
    }

}
