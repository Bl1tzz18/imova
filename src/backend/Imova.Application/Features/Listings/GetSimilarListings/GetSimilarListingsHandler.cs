using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.GetSimilarListings;

// One query per stage, tightest first; a stage is only broadened when it found fewer than
// MinResults. Each stage is a superset of the one before and ranks the same way, so the last
// query's results are the answer — whatever it found, even fewer than MinResults.
public class GetSimilarListingsHandler(
    IApplicationDbContext dbContext, IBlobStorageService blobStorageService, ISimilarListingsFinder finder)
    : IRequestHandler<GetSimilarListingsQuery, IReadOnlyList<ListingDto>?>
{
    public async Task<IReadOnlyList<ListingDto>?> Handle(GetSimilarListingsQuery request, CancellationToken cancellationToken)
    {
        var source = await (
                from l in dbContext.Listings.AsNoTracking()
                join p in dbContext.Properties.AsNoTracking() on l.PropertyId equals p.Id
                join loc in dbContext.PropertyLocations.AsNoTracking() on p.LocationId equals loc.Id
                where l.Id == request.ListingId && l.Status == ListingStatus.Active
                select new { Listing = l, Property = p, loc.RaionId, loc.ChisinauSectorId, loc.LocalitateId })
            .FirstOrDefaultAsync(cancellationToken);

        if (source is null)
        {
            return null;
        }

        var target = SimilarListingTarget.For(
            source.Listing, source.Property, source.RaionId, source.ChisinauSectorId, source.LocalitateId);

        IReadOnlyList<Guid> ids = [];
        foreach (var stage in SimilarListingStages.For(target))
        {
            ids = await finder.FindAsync(target, stage, SimilarListingStages.MaxResults, cancellationToken);
            if (ids.Count >= SimilarListingStages.MinResults)
            {
                break;
            }
        }

        var byId = await dbContext.Listings.AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);
        var ordered = ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();

        return await ListingDtoLoader.LoadAsync(dbContext, blobStorageService, ordered, request.CurrentUserId, cancellationToken);
    }
}
