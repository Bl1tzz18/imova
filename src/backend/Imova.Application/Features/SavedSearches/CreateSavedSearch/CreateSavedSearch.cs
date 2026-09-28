using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Contracts.SavedSearches;
using Imova.Domain.SavedSearches;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.SavedSearches.CreateSavedSearch;

// Saves the /search page's current query string. Saving the same search again (same filters,
// in any order) updates the existing one's name and alerts instead of adding a duplicate.
public record CreateSavedSearchCommand(Guid UserId, string Name, string QueryString, AlertFrequency AlertFrequency)
    : IRequest<SavedSearchDto>;

public class CreateSavedSearchValidator : AbstractValidator<CreateSavedSearchCommand>
{
    public CreateSavedSearchValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(SavedSearch.MaxNameLength);
        RuleFor(c => c.QueryString).NotNull().MaximumLength(SavedSearch.MaxQueryLength);
        RuleFor(c => c.AlertFrequency).IsInEnum();
    }
}

public class CreateSavedSearchHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<CreateSavedSearchCommand, SavedSearchDto>
{
    public async Task<SavedSearchDto> Handle(CreateSavedSearchCommand request, CancellationToken cancellationToken)
    {
        // Only a search the search endpoint itself would accept can be saved.
        var queryString = SearchQueryString.Normalize(request.QueryString);
        var search = new SearchListingsValidator().Validate(SearchQueryString.Parse(queryString));
        if (!search.IsValid)
        {
            throw new ValidationException(search.Errors);
        }

        var existing = await dbContext.SavedSearches
            .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.QueryString == queryString, cancellationToken);
        if (existing is not null)
        {
            existing.Rename(request.Name);
            existing.ChangeAlertFrequency(request.AlertFrequency);
            await dbContext.SaveChangesAsync(cancellationToken);
            return existing.ToDto(0);
        }

        if (await dbContext.SavedSearches.CountAsync(s => s.UserId == request.UserId, cancellationToken) >= SavedSearchRules.MaxPerUser)
        {
            throw new ValidationException(
            [
                CodedFailure.Of(
                    nameof(CreateSavedSearchCommand.QueryString),
                    $"You can save at most {SavedSearchRules.MaxPerUser} searches. Delete one to save another.",
                    ErrorCodes.SavedSearchLimit,
                    CodedFailure.Params(("max", SavedSearchRules.MaxPerUser))),
            ]);
        }

        var savedSearch = SavedSearch.Create(
            request.UserId, request.Name, queryString, request.AlertFrequency, timeProvider.GetUtcNow());
        dbContext.SavedSearches.Add(savedSearch);
        await dbContext.SaveChangesAsync(cancellationToken);
        return savedSearch.ToDto(0);
    }
}
