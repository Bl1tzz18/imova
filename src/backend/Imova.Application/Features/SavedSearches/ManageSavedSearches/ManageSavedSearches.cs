using FluentValidation;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Contracts.SavedSearches;
using Imova.Domain.SavedSearches;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.SavedSearches.ManageSavedSearches;

// The signed-in user's own saved searches: list, rename / change alerts, delete, mark as viewed.
// Another user's saved search is simply not found (null / false → 404).

public record GetSavedSearchesQuery(Guid UserId) : IRequest<IReadOnlyList<SavedSearchDto>>;

public class GetSavedSearchesHandler(IApplicationDbContext dbContext, IListingSearch listingSearch)
    : IRequestHandler<GetSavedSearchesQuery, IReadOnlyList<SavedSearchDto>>
{
    public async Task<IReadOnlyList<SavedSearchDto>> Handle(GetSavedSearchesQuery request, CancellationToken cancellationToken)
    {
        var savedSearches = await dbContext.SavedSearches.AsNoTracking()
            .Where(s => s.UserId == request.UserId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        // One count query per search — at most SavedSearchRules.MaxPerUser.
        var result = new List<SavedSearchDto>(savedSearches.Count);
        foreach (var savedSearch in savedSearches)
        {
            result.Add(savedSearch.ToDto(await SavedSearchMatches.CountNewAsync(listingSearch, savedSearch, cancellationToken)));
        }

        return result;
    }
}

public record UpdateSavedSearchCommand(Guid UserId, Guid Id, string Name, AlertFrequency AlertFrequency) : IRequest<SavedSearchDto?>;

public class UpdateSavedSearchValidator : AbstractValidator<UpdateSavedSearchCommand>
{
    public UpdateSavedSearchValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(SavedSearch.MaxNameLength);
        RuleFor(c => c.AlertFrequency).IsInEnum();
    }
}

public class UpdateSavedSearchHandler(IApplicationDbContext dbContext, IListingSearch listingSearch)
    : IRequestHandler<UpdateSavedSearchCommand, SavedSearchDto?>
{
    public async Task<SavedSearchDto?> Handle(UpdateSavedSearchCommand request, CancellationToken cancellationToken)
    {
        var savedSearch = await dbContext.SavedSearches
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == request.UserId, cancellationToken);
        if (savedSearch is null)
        {
            return null;
        }

        savedSearch.Rename(request.Name);
        savedSearch.ChangeAlertFrequency(request.AlertFrequency);
        await dbContext.SaveChangesAsync(cancellationToken);
        return savedSearch.ToDto(await SavedSearchMatches.CountNewAsync(listingSearch, savedSearch, cancellationToken));
    }
}

public record DeleteSavedSearchCommand(Guid UserId, Guid Id) : IRequest<bool>;

public class DeleteSavedSearchHandler(IApplicationDbContext dbContext) : IRequestHandler<DeleteSavedSearchCommand, bool>
{
    public async Task<bool> Handle(DeleteSavedSearchCommand request, CancellationToken cancellationToken)
    {
        var savedSearch = await dbContext.SavedSearches
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == request.UserId, cancellationToken);
        if (savedSearch is null)
        {
            return false;
        }

        dbContext.SavedSearches.Remove(savedSearch);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

// The user opened the search — its "new since your last visit" count starts again from now.
public record MarkSavedSearchViewedCommand(Guid UserId, Guid Id) : IRequest<SavedSearchDto?>;

public class MarkSavedSearchViewedHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<MarkSavedSearchViewedCommand, SavedSearchDto?>
{
    public async Task<SavedSearchDto?> Handle(MarkSavedSearchViewedCommand request, CancellationToken cancellationToken)
    {
        var savedSearch = await dbContext.SavedSearches
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == request.UserId, cancellationToken);
        if (savedSearch is null)
        {
            return null;
        }

        savedSearch.MarkViewed(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return savedSearch.ToDto(0);
    }
}
