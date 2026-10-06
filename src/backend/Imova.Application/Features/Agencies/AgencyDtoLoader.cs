using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.Logos;
using Imova.Application.Features.Listings;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies;

// Builds an AgencyDto for whoever is asking (the agency loaded with its Members): the phone number
// only for members and site admins, the caller's own role, and the counts.
public static class AgencyDtoLoader
{
    public static async Task<AgencyDto> LoadAsync(
        IApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        Agency agency,
        Guid? currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var raionName = agency.RaionId is { } raionId
            ? await dbContext.Raioane.AsNoTracking().Where(r => r.Id == raionId).Select(r => r.NameRo).FirstOrDefaultAsync(cancellationToken)
            : null;
        var activeListings = await dbContext.Listings.AsNoTracking()
            .CountAsync(l => l.AgencyId == agency.Id && l.Status == ListingStatus.Active, cancellationToken);

        var myRole = currentUserId is { } userId ? agency.RoleOf(userId) : null;
        var canSeePhone = isAdmin || myRole is not null;
        var shape = PhoneShape.For(agency.Phone);

        return new AgencyDto(
            agency.Id,
            agency.Slug,
            agency.Name,
            agency.LogoBlobName is { } logo ? blobStorageService.GetPublicUrl(logo) : null,
            agency.LogoBlobName is { } large ? blobStorageService.GetPublicUrl(AgencyLogo.ThumbnailBlobName(large)) : null,
            agency.Bio,
            canSeePhone ? agency.Phone : null,
            shape?.Prefix,
            shape?.HiddenDigits,
            agency.Email,
            agency.Website,
            agency.Address,
            agency.RaionId,
            raionName,
            agency.IsVerified,
            agency.VerifiedAt,
            agency.Status.ToString(),
            agency.CreatedAt,
            agency.Members.Count,
            activeListings,
            myRole?.ToString());
    }

    // The agency with its members (tracked, for a handler about to change it), or null.
    public static Task<Agency?> FindAsync(IApplicationDbContext dbContext, Guid agencyId, CancellationToken cancellationToken) =>
        dbContext.Agencies.Include(a => a.Members).FirstOrDefaultAsync(a => a.Id == agencyId, cancellationToken);
}
