using Imova.Application.Common.Interfaces;
using Imova.Contracts.Sitemap;
using Imova.Domain.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Sitemap;

// The public agency pages (/agencies/{slug}) for sitemap.xml: active agencies, like the directory.
public record GetSitemapAgenciesQuery : IRequest<IReadOnlyList<SitemapAgencyDto>>;

public class GetSitemapAgenciesHandler(IApplicationDbContext dbContext) : IRequestHandler<GetSitemapAgenciesQuery, IReadOnlyList<SitemapAgencyDto>>
{
    public async Task<IReadOnlyList<SitemapAgencyDto>> Handle(GetSitemapAgenciesQuery request, CancellationToken cancellationToken) =>
        await dbContext.Agencies.AsNoTracking()
            .Where(a => a.Status == AgencyStatus.Active)
            .OrderBy(a => a.Slug)
            .Select(a => new SitemapAgencyDto(a.Slug, a.UpdatedAt))
            .ToListAsync(cancellationToken);
}
