using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies;

// Picks a free slug for an agency name: AgencySlug.FromName, else the same numbered -2, -3, … A
// slug is taken when another agency uses it now or used it before (AgencySlugHistory — old links
// must keep finding that agency). The agency's own current and former slugs count as free, so
// renaming back reclaims the old address. Two agencies created at the same moment with the same
// name can still collide; the unique index then refuses the second.
public static class AgencySlugs
{
    public static async Task<string> FreeSlugAsync(
        IApplicationDbContext dbContext, string name, Guid? agencyId, CancellationToken cancellationToken)
    {
        var wanted = AgencySlug.FromName(name);

        // Every slug that could clash — the wanted one and its numbered forms, which WithNumber may
        // shorten by up to 10 characters — in two queries.
        var prefix = wanted.Length > AgencySlug.MaxLength - 10 ? wanted[..(AgencySlug.MaxLength - 10)] : wanted;
        var taken = (await dbContext.Agencies.AsNoTracking()
                .Where(a => a.Slug.StartsWith(prefix) && a.Id != agencyId)
                .Select(a => a.Slug)
                .ToListAsync(cancellationToken))
            .Concat(await dbContext.AgencyFormerSlugs.AsNoTracking()
                .Where(s => s.Slug.StartsWith(prefix) && s.AgencyId != agencyId)
                .Select(s => s.Slug)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        if (!taken.Contains(wanted))
        {
            return wanted;
        }

        for (var number = 2; ; number++)
        {
            var candidate = AgencySlug.WithNumber(wanted, number);
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
