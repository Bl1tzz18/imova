using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Auth.Login;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Account.DeleteAccount;

public class DeleteAccountHandler(UserManager<ApplicationUser> userManager, AccountDeletion accountDeletion)
    : IRequestHandler<DeleteAccountCommand>
{
    public const string WrongPassword = "The password is incorrect.";

    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.", ErrorCodes.UserNotFound);

        // A signed-in session alone isn't enough for something this final: a borrowed laptop or a
        // stolen session token mustn't be able to erase the account.
        if (!await userManager.HasPasswordAsync(user))
        {
            throw new ValidationException(
            [
                CodedFailure.Of(
                    nameof(DeleteAccountCommand.Password),
                    "This account has no password — confirm the deletion through the emailed link.",
                    ErrorCodes.AccountNoPassword),
            ]);
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            throw new ValidationException(
            [
                CodedFailure.Of(nameof(DeleteAccountCommand.Password), "Enter your password to delete the account.", ErrorCodes.AccountPasswordRequired),
            ]);
        }

        // Same lockout as sign-in (LoginHandler), so this isn't a way around it to guess passwords.
        if (await userManager.IsLockedOutAsync(user))
        {
            throw new TooManyRequestsException(LoginHandler.LockedOut, ErrorCodes.LockedOut);
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            if (await userManager.IsLockedOutAsync(user))
            {
                throw new TooManyRequestsException(LoginHandler.LockedOut, ErrorCodes.LockedOut);
            }

            throw new ValidationException(
            [
                CodedFailure.Of(nameof(DeleteAccountCommand.Password), WrongPassword, ErrorCodes.AccountWrongPassword),
            ]);
        }

        await accountDeletion.DeleteAsync(user.Id, cancellationToken);
    }
}
