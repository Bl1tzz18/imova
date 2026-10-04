using Imova.Application.Common.Interfaces;
using Imova.Contracts.Agencies;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Agencies.Logos;

public class RemoveAgencyLogoHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    TimeProvider timeProvider,
    ILogger<RemoveAgencyLogoHandler> logger) : IRequestHandler<RemoveAgencyLogoCommand, AgencyDto?>
{
    public async Task<AgencyDto?> Handle(RemoveAgencyLogoCommand request, CancellationToken cancellationToken)
    {
        var agency = await AgencyDtoLoader.FindAsync(dbContext, request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.EnsureCanEdit(agency, request.UserId, request.IsAdmin))
        {
            return null;
        }

        if (agency.RemoveLogo(timeProvider.GetUtcNow()) is { } removed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await AgencyLogo.DeleteFilesAsync(blobStorageService, removed, logger, cancellationToken);
        }

        return await AgencyDtoLoader.LoadAsync(dbContext, blobStorageService, agency, request.UserId, request.IsAdmin, cancellationToken);
    }
}
