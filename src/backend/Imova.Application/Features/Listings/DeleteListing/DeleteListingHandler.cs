using Imova.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Listings.DeleteListing;

public class DeleteListingHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    ILogger<DeleteListingHandler> logger) : IRequestHandler<DeleteListingCommand, bool>
{
    public async Task<bool> Handle(DeleteListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
        if (listing is null)
        {
            return false;
        }

        await ListingAccess.EnsureCanManageAsync(dbContext, listing, request.RequestingUserId, request.IsAdmin, cancellationToken);

        var blobNames = await ListingRemoval.RemoveAsync(dbContext, [listing], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var blobName in blobNames)
        {
            try
            {
                await blobStorageService.DeleteAsync(blobName, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not delete blob {BlobName} of deleted listing {ListingId}.", blobName, listing.Id);
            }
        }

        return true;
    }
}
