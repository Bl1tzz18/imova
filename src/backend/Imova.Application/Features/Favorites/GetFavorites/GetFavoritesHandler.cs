using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Contracts.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Favorites.GetFavorites;

public class GetFavoritesHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetFavoritesQuery, List<ListingDto>>
{
    public async Task<List<ListingDto>> Handle(GetFavoritesQuery request, CancellationToken cancellationToken)
    {
        var favoriteListingIds = await dbContext.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == request.UserId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.ListingId)
            .ToListAsync(cancellationToken);

        if (favoriteListingIds.Count == 0)
        {
            return [];
        }

        var listingsById = await dbContext.Listings
            .AsNoTracking()
            .Where(l => favoriteListingIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        // Preserve the "most recently saved first" order — Listings.Where(...) makes no ordering
        // guarantee of its own. Every listing is saved by definition.
        var listings = favoriteListingIds.Where(listingsById.ContainsKey).Select(id => listingsById[id]).ToList();
        return await ListingDtoLoader.LoadAsync(dbContext, blobStorageService, listings, request.UserId, cancellationToken);
    }
}
