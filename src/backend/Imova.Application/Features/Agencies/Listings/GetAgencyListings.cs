using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Contracts.Listings;
using Imova.Domain.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Listings;

// The agency's listings in every status (drafts, in review, ended…) for its management page — the
// public only ever sees the Active ones, through search's agencyId. Owners, Admins and site admins
// get all of them; an Agent only the ones they wrote (the ones they may manage, see ListingAccess).
// Null (404) when the caller may not see the agency at all; 403 for a signed-in non-member. Newest
// first.
public record GetAgencyListingsQuery(Guid AgencyId, Guid UserId, bool IsAdmin) : IRequest<List<ListingDto>?>;

public class GetAgencyListingsHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetAgencyListingsQuery, List<ListingDto>?>
{
    public async Task<List<ListingDto>?> Handle(GetAgencyListingsQuery request, CancellationToken cancellationToken)
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

        var rows = await listings
            .OrderByDescending(l => l.CreatedAt)
            .ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);

        return await ListingDtoLoader.LoadAsync(
            dbContext, blobStorageService, rows, request.UserId, cancellationToken, viewerIsAdmin: request.IsAdmin);
    }
}
