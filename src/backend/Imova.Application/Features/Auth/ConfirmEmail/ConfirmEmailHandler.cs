using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Listings;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.ConfirmEmail;

public class ConfirmEmailHandler(UserManager<ApplicationUser> userManager, IApplicationDbContext dbContext)
    : IRequestHandler<ConfirmEmailCommand>
{
    public const string InvalidLink = "This confirmation link is invalid or has expired. Request a new one.";

    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString()) ?? throw InvalidLinkError();

        // Opening the link twice (or after confirming through a password reset) is fine.
        if (user.EmailConfirmed)
        {
            return;
        }

        var token = AccountTokens.Decode(request.Token) ?? throw InvalidLinkError();
        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            throw InvalidLinkError();
        }

        // Listings created while unconfirmed were waiting as Drafts (see ReviewEligibility).
        await ReviewEligibility.SubmitWaitingDraftsAsync(dbContext, user.Id, cancellationToken);
    }

    private static ValidationException InvalidLinkError() =>
        new([CodedFailure.Of(nameof(ConfirmEmailCommand.Token), InvalidLink, ErrorCodes.InvalidLink)]);
}
