using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Media.DeleteMedia;

public class DeleteMediaHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<DeleteMediaCommand, bool>
{
    public async Task<bool> Handle(DeleteMediaCommand request, CancellationToken cancellationToken)
    {
        var media = await dbContext.PropertyMedias
            .FirstOrDefaultAsync(m => m.Id == request.MediaId && m.PropertyId == request.PropertyId, cancellationToken);

        if (media is null)
        {
            return false;
        }

        var property = await dbContext.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId, cancellationToken);

        if (property is null)
        {
            return false;
        }

        if (!request.IsAdmin && property.OwnerId != request.RequestingUserId)
        {
            throw new ForbiddenAccessException();
        }

        dbContext.PropertyMedias.Remove(media);
        await dbContext.SaveChangesAsync(cancellationToken);

        await blobStorageService.DeleteAsync(media.BlobName, cancellationToken);

        return true;
    }
}
