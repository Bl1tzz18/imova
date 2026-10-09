using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Listings;

// The agency's listings in every status (drafts, in review, ended…) for its management page, one page
// of one tab at a time — the public only ever sees the Active ones, through search's agencyId. Owners,
// Admins and site admins get all of them; an Agent only the ones they wrote (the ones they may manage,
// see ListingAccess). Null (404) when the caller may not see the agency at all; 403 for a signed-in
// non-member.
//
// Grouping, search and order follow AgencyListingGroups over a light row per listing (an agency may
// have hundreds); only the page shown is built into ListingDtos. Group: Active | Unpublished | Ended
// (default: Active, else the first with listings); Sort: Recommended | Newest | PriceAsc | PriceDesc;
// a page past the end shows the last one.
public record GetAgencyListingsQuery(
    Guid AgencyId,
    Guid UserId,
    bool IsAdmin,
    string? Group = null,
    string? Query = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = GetAgencyListingsQuery.DefaultPageSize) : IRequest<AgencyListingsPageDto?>
{
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 60;
}

public class GetAgencyListingsHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService, TimeProvider timeProvider)
    : IRequestHandler<GetAgencyListingsQuery, AgencyListingsPageDto?>
{
    public async Task<AgencyListingsPageDto?> Handle(GetAgencyListingsQuery request, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.AsNoTracking().Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.CanView(agency, request.UserId, request.IsAdmin))
        {
            return null;
        }

        var role = agency.RoleOf(request.UserId);
        if (!request.IsAdmin && role is null)
        {
            throw new ForbiddenAccessException();
        }

        var listings = dbContext.Listings.AsNoTracking().Where(l => l.AgencyId == agency.Id);
        if (!request.IsAdmin && role is AgencyRole.Agent)
        {
            var myPublisherIds = dbContext.Publishers.Where(p => p.UserId == request.UserId).Select(p => p.Id);
            listings = listings.Where(l => myPublisherIds.Contains(l.PublisherId));
        }

        var rows = await (
                from l in listings
                join p in dbContext.Properties.AsNoTracking() on l.PropertyId equals p.Id
                join loc in dbContext.PropertyLocations.AsNoTracking() on p.LocationId equals loc.Id into locations
                from loc in locations.DefaultIfEmpty()
                select new AgencyListingRow(
                    l.Id,
                    l.Title,
                    l.Status,
                    l.ExpiresAt,
                    l.CreatedAt,
                    l.UpdatedAt,
                    l.Price.PriceEur,
                    loc == null ? null : loc.RaionName,
                    loc == null ? null : loc.LocalitateName,
                    loc == null ? null : loc.ChisinauSectorName,
                    loc == null ? null : loc.Street))
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var matching = rows.Where(r => AgencyListingGroups.Matches(r, request.Query)).ToList();
        var byGroup = Enum.GetValues<AgencyListingGroup>()
            .ToDictionary(g => g, g => matching.Where(r => AgencyListingGroups.GroupOf(r.Status) == g).ToList());

        var group = AgencyListingGroups.InitialGroup(
            AgencyListingGroups.ParseGroup(request.Group), byGroup.ToDictionary(kv => kv.Key, kv => kv.Value.Count));
        var inGroup = AgencyListingGroups.Order(byGroup[group], AgencyListingGroups.ParseSort(request.Sort), now).ToList();

        var pageSize = Math.Clamp(request.PageSize, 1, GetAgencyListingsQuery.MaxPageSize);
        var lastPage = Math.Max(1, (inGroup.Count + pageSize - 1) / pageSize);
        var page = Math.Clamp(request.Page, 1, lastPage);
        var pageIds = inGroup.Skip((page - 1) * pageSize).Take(pageSize).Select(r => r.Id).ToList();

        var entities = await dbContext.Listings.AsNoTracking()
            .Where(l => pageIds.Contains(l.Id))
            .ToListAsync(cancellationToken);
        var ordered = pageIds.Select(id => entities.Single(l => l.Id == id)).ToList();
        var items = await ListingDtoLoader.LoadAsync(
            dbContext, blobStorageService, ordered, request.UserId, cancellationToken, viewerIsAdmin: request.IsAdmin);

        AgencyListingGroupDto Summary(AgencyListingGroup g) =>
            new(byGroup[g].Count, byGroup[g].Any(r => AgencyListingGroups.NeedsAttention(r, now)));

        return new AgencyListingsPageDto(
            items,
            group.ToString(),
            page,
            pageSize,
            inGroup.Count,
            Summary(AgencyListingGroup.Active),
            Summary(AgencyListingGroup.Unpublished),
            Summary(AgencyListingGroup.Ended));
    }
}
