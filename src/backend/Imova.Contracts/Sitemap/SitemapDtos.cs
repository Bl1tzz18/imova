namespace Imova.Contracts.Sitemap;

// What the web app's sitemap.xml lists: every public listing (by its number and title — together
// its address, /property/100015-…) and every public agency page, each with when it last changed.

public record SitemapListingDto(long Number, string Title, DateTimeOffset LastModified);

// One sitemap file's worth of listings, oldest number first. Total says how many files there are.
public record SitemapListingPageDto(int Total, int PageSize, IReadOnlyList<SitemapListingDto> Items);

public record SitemapAgencyDto(string Slug, DateTimeOffset LastModified);
