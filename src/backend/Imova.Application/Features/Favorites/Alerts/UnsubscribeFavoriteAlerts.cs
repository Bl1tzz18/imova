using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Favorites.Alerts;

// The "stop these emails" link in a saved-listing email: turns those emails off for the account
// (ApplicationUser.EmailFavoriteUpdates — the same switch as the account settings), no sign-in
// needed — the token proves the link came from us. The favorites themselves stay. False when the
// account is gone (nothing left to email). Says nothing about the account: the link never expires
// and may be forwarded.
public record UnsubscribeFavoriteAlertsCommand(Guid UserId, string Token) : IRequest<bool>;

public class UnsubscribeFavoriteAlertsValidator : AbstractValidator<UnsubscribeFavoriteAlertsCommand>
{
    public UnsubscribeFavoriteAlertsValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Token).NotEmpty().MaximumLength(1000);
    }
}

public class UnsubscribeFavoriteAlertsHandler(IApplicationDbContext dbContext, FavoriteAlertUnsubscribeTokens tokens)
    : IRequestHandler<UnsubscribeFavoriteAlertsCommand, bool>
{
    public async Task<bool> Handle(UnsubscribeFavoriteAlertsCommand request, CancellationToken cancellationToken)
    {
        if (!tokens.IsValid(request.UserId, request.Token))
        {
            throw new ValidationException(
                [CodedFailure.Of(nameof(UnsubscribeFavoriteAlertsCommand.Token), "This link is invalid.", ErrorCodes.InvalidLink)]);
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.EmailFavoriteUpdates = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
