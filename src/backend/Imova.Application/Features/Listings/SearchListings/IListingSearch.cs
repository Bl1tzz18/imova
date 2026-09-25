namespace Imova.Application.Features.Listings.SearchListings;

// Runs the search in the database (Imova.Infrastructure/Listings/ListingSearch): the ids of one
// page of matching listings, in sort order, and how many match in total. Lives behind an interface
// because the type-specific and rental filters query JSONB columns with provider-specific SQL.
public interface IListingSearch
{
    Task<(IReadOnlyList<Guid> ListingIds, int TotalCount)> SearchAsync(SearchListingsQuery query, CancellationToken cancellationToken);
}
