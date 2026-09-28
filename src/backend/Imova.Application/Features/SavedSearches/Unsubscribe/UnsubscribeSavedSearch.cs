using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Contracts.SavedSearches;
using Imova.Domain.SavedSearches;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.SavedSearches.Unsubscribe;

// The "stop these emails" link in an alert email: turns that search's alerts off, no sign-in
// needed — the token proves the link came from us (SavedSearchUnsubscribeTokens). The search itself
// stays saved. A deleted search counts as already unsubscribed.
public record UnsubscribeSavedSearchCommand(Guid Id, string Token) : IRequest<SavedSearchUnsubscribedDto?>;

public class UnsubscribeSavedSearchValidator : AbstractValidator<UnsubscribeSavedSearchCommand>
{
    public UnsubscribeSavedSearchValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Token).NotEmpty().MaximumLength(1000);
    }
}

public class UnsubscribeSavedSearchHandler(IApplicationDbContext dbContext, SavedSearchUnsubscribeTokens tokens)
    : IRequestHandler<UnsubscribeSavedSearchCommand, SavedSearchUnsubscribedDto?>
{
    public async Task<SavedSearchUnsubscribedDto?> Handle(UnsubscribeSavedSearchCommand request, CancellationToken cancellationToken)
    {
        if (!tokens.IsValid(request.Id, request.Token))
        {
            throw new ValidationException(
                [CodedFailure.Of(nameof(UnsubscribeSavedSearchCommand.Token), "This link is invalid.", ErrorCodes.InvalidLink)]);
        }

        var savedSearch = await dbContext.SavedSearches.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (savedSearch is null)
        {
            return null;
        }

        savedSearch.ChangeAlertFrequency(AlertFrequency.Off);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SavedSearchUnsubscribedDto(savedSearch.Name);
    }
}
