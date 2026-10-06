using Imova.Application.Common.Interfaces;
using Imova.Contracts.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.GetAgency;

// The public agency page (/agencies/{slug}). A current slug → the agency; a slug it used before a
// rename → only its current slug (the page answers with a permanent redirect, so old links and
// search results keep working). Null (404) for an unknown slug, or one of an agency the caller may
// not see (deactivated, and they're neither a member nor an admin) — current or former alike.
public record GetAgencyBySlugQuery(string Slug, Guid? UserId, bool IsAdmin) : IRequest<AgencyBySlugResult?>;

// Exactly one of the two is set.
public record AgencyBySlugResult(AgencyDto? Agency, string? CurrentSlug);

public class GetAgencyBySlugHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetAgencyBySlugQuery, AgencyBySlugResult?>
{
    public async Task<AgencyBySlugResult?> Handle(GetAgencyBySlugQuery request, CancellationToken cancellationToken)
    {
        // Slugs are stored in lower case; a link typed with capitals still finds the agency.
        var slug = request.Slug.Trim().ToLowerInvariant();

        var agency = await dbContext.Agencies.AsNoTracking()
            .Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Slug == slug, cancellationToken);
        if (agency is not null)
        {
            return AgencyAccess.CanView(agency, request.UserId, request.IsAdmin)
                ? new AgencyBySlugResult(
                    await AgencyDtoLoader.LoadAsync(dbContext, blobStorageService, agency, request.UserId, request.IsAdmin, cancellationToken),
                    null)
                : null;
        }

        var renamedId = await dbContext.AgencyFormerSlugs.AsNoTracking()
            .Where(s => s.Slug == slug)
            .Select(s => (Guid?)s.AgencyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (renamedId is null)
        {
            return null;
        }

        var renamed = await dbContext.Agencies.AsNoTracking()
            .Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == renamedId, cancellationToken);
        return renamed is not null && AgencyAccess.CanView(renamed, request.UserId, request.IsAdmin)
            ? new AgencyBySlugResult(null, renamed.Slug)
            : null;
    }
}
