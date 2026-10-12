using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.CreateAgency;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.UpdateAgency;

public class UpdateAgencyHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    TimeProvider timeProvider,
    IDatabaseErrors databaseErrors) : IRequestHandler<UpdateAgencyCommand, AgencyDto?>
{
    // The slug history's key: a renamed agency's old slug — which another agency may be renamed to (or
    // away from) at the same moment.
    public const string SlugHistoryKey = "PK_AgencySlugHistory";

    public async Task<AgencyDto?> Handle(UpdateAgencyCommand request, CancellationToken cancellationToken)
    {
        var agency = await AgencyDtoLoader.FindAsync(dbContext, request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.EnsureCanEdit(agency, request.UserId, request.IsAdmin))
        {
            return null;
        }

        var profile = await AgencyProfileValidator<UpdateAgencyCommand>.ProfileAsync(dbContext, request, request.Email!, cancellationToken);
        var now = timeProvider.GetUtcNow();

        // Only a new name moves the agency to a new address.
        var slug = string.Equals(request.Name.Trim(), agency.Name, StringComparison.Ordinal)
            ? agency.Slug
            : await AgencySlugs.FreeSlugAsync(dbContext, request.Name, agency.Id, cancellationToken);

        // Renamed back to an earlier name: that slug is current again, no longer a former one.
        var reclaimed = await dbContext.AgencyFormerSlugs.FirstOrDefaultAsync(s => s.Slug == slug && s.AgencyId == agency.Id, cancellationToken);
        if (reclaimed is not null)
        {
            dbContext.AgencyFormerSlugs.Remove(reclaimed);
        }

        if (agency.UpdateProfile(profile, slug, now) is { } replaced)
        {
            dbContext.AgencyFormerSlugs.Add(AgencyFormerSlug.Create(replaced, agency.Id, now));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            databaseErrors.IsUniqueViolation(ex, CreateAgencyHandler.SlugIndex) || databaseErrors.IsUniqueViolation(ex, SlugHistoryKey))
        {
            // Another agency took the same address (slug) between our check and our save — two renames,
            // or a create and a rename, at the same moment. Nothing was saved; a 409 the form shows,
            // and saving again picks a free address.
            throw new ConflictException(
                "Another agency took this name's address a moment ago; save again.", ErrorCodes.AgencyNameConflict);
        }

        return await AgencyDtoLoader.LoadAsync(dbContext, blobStorageService, agency, request.UserId, request.IsAdmin, cancellationToken);
    }
}
