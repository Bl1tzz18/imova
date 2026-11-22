using FluentValidation;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Contracts.Sitemap;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Sitemap;

// The public listings for sitemap.xml, PageSize per file (search engines take at most 50 000
// addresses per file; each listing also carries its two other languages, so this stays well under
// any size limit). Only what the public may see (ListingVisibility), in number order so a page
// keeps its listings between requests.
public record GetSitemapListingsQuery(int Page = 1) : IRequest<SitemapListingPageDto>
{
    public const int PageSize = 10_000;
}

public class GetSitemapListingsValidator : AbstractValidator<GetSitemapListingsQuery>
{
    public GetSitemapListingsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
    }
}

public class GetSitemapListingsHandler(IApplicationDbContext dbContext) : IRequestHandler<GetSitemapListingsQuery, SitemapListingPageDto>
{
    public async Task<SitemapListingPageDto> Handle(GetSitemapListingsQuery request, CancellationToken cancellationToken)
    {
        var listings = dbContext.Listings.AsNoTracking().WherePublic(dbContext);

        var total = await listings.CountAsync(cancellationToken);
        var items = await listings
            .OrderBy(l => l.Number)
            .Skip((request.Page - 1) * GetSitemapListingsQuery.PageSize)
            .Take(GetSitemapListingsQuery.PageSize)
            .Select(l => new SitemapListingDto(l.Number, l.Title, l.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new SitemapListingPageDto(total, GetSitemapListingsQuery.PageSize, items);
    }
}
