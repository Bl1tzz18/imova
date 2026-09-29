using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Account.ConfirmAccountDeletion;

public class ConfirmAccountDeletionHandler(UserManager<ApplicationUser> userManager, AccountDeletion accountDeletion)
    : IRequestHandler<ConfirmAccountDeletionCommand>
{
    public const string InvalidLink = "This link is invalid or has expired, or the account was already deleted.";

    public async Task Handle(ConfirmAccountDeletionCommand request, CancellationToken cancellationToken)
    {
        // An unknown account and a bad token look the same, so the link reveals nothing.
        var user = await userManager.FindByIdAsync(request.UserId.ToString()) ?? throw InvalidLinkError();
        var token = AccountTokens.Decode(request.Token) ?? throw InvalidLinkError();
        if (!await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, AccountDeletion.TokenPurpose, token))
        {
            throw InvalidLinkError();
        }

        await accountDeletion.DeleteAsync(user.Id, cancellationToken);
    }

    private static ValidationException InvalidLinkError() =>
        new([CodedFailure.Of(nameof(ConfirmAccountDeletionCommand.Token), InvalidLink, ErrorCodes.InvalidLink)]);
}
