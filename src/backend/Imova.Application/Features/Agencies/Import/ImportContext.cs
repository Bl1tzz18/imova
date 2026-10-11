using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Import;

// What every row of one import needs, loaded once: the agency (with its members), who the listings'
// author is by default, the members by email (agent_email), the amenity and proximity keys, and the
// location index.
//
// Author: whoever uploads, when they're a member (an Owner or Admin — ImportAccess); a site admin who
// isn't a member (importing for an agency during onboarding) makes the agency's longest-standing
// Owner the author. agent_email naming a current member overrides either.
public sealed class ImportContext
{
    private ImportContext(
        Agency agency,
        Guid defaultAuthorId,
        IReadOnlyDictionary<string, Guid> memberIdsByEmail,
        IReadOnlyDictionary<string, Guid> amenityIdsByKey,
        IReadOnlyDictionary<string, Guid> proximityIdsByKey,
        ImportLocationIndex locations)
    {
        Agency = agency;
        DefaultAuthorId = defaultAuthorId;
        MemberIdsByEmail = memberIdsByEmail;
        AmenityIdsByKey = amenityIdsByKey;
        ProximityIdsByKey = proximityIdsByKey;
        Locations = locations;
    }

    public Agency Agency { get; }

    public Guid DefaultAuthorId { get; }

    // Upper-cased emails (Identity's NormalizedEmail) of the agency's current members.
    public IReadOnlyDictionary<string, Guid> MemberIdsByEmail { get; }

    public IReadOnlyDictionary<string, Guid> AmenityIdsByKey { get; }

    public IReadOnlyDictionary<string, Guid> ProximityIdsByKey { get; }

    public ImportLocationIndex Locations { get; }

    public static async Task<ImportContext> LoadAsync(
        IApplicationDbContext dbContext, Guid agencyId, Guid uploaderUserId, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.AsNoTracking().Include(a => a.Members)
            .FirstAsync(a => a.Id == agencyId, cancellationToken);

        var memberIds = agency.Members.Select(m => m.UserId).ToList();
        var memberIdsByEmail = (await dbContext.Users.AsNoTracking()
                .Where(u => memberIds.Contains(u.Id) && u.NormalizedEmail != null)
                .Select(u => new { u.Id, u.NormalizedEmail })
                .ToListAsync(cancellationToken))
            .ToDictionary(u => u.NormalizedEmail!, u => u.Id, StringComparer.Ordinal);

        var amenities = await dbContext.Amenities.AsNoTracking().ToDictionaryAsync(a => a.Key, a => a.Id, cancellationToken);
        var proximities = await dbContext.Proximities.AsNoTracking().ToDictionaryAsync(p => p.Key, p => p.Id, cancellationToken);

        return new ImportContext(
            agency,
            DefaultAuthorFor(agency, uploaderUserId),
            memberIdsByEmail,
            new Dictionary<string, Guid>(amenities, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, Guid>(proximities, StringComparer.OrdinalIgnoreCase),
            await ImportLocationIndex.LoadAsync(dbContext, cancellationToken));
    }

    public static Guid DefaultAuthorFor(Agency agency, Guid uploaderUserId) =>
        agency.IsMember(uploaderUserId)
            ? uploaderUserId
            : agency.Members.Where(m => m.Role == AgencyRole.Owner).OrderBy(m => m.JoinedAt).ThenBy(m => m.UserId).First().UserId;

    // agent_email's member, or null when it names nobody in the agency.
    public Guid? MemberByEmail(string email) =>
        MemberIdsByEmail.TryGetValue(email.Trim().ToUpperInvariant(), out var userId) ? userId : null;
}
