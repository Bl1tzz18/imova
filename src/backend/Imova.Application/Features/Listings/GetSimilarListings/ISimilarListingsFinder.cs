namespace Imova.Application.Features.Listings.GetSimilarListings;

// Runs one stage in the database (Imova.Infrastructure/Listings/SimilarListingsFinder): the ids of
// up to `limit` matching listings, best first. Behind an interface because the room count lives in
// the JSONB attributes, compared with provider-specific SQL.
public interface ISimilarListingsFinder
{
    Task<IReadOnlyList<Guid>> FindAsync(
        SimilarListingTarget target, SimilarListingStage stage, int limit, CancellationToken cancellationToken);
}
