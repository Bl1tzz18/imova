using Imova.Application.Common.Interfaces;
using Imova.Contracts.Media;
using Imova.Domain.Media;

namespace Imova.Application.Features.Media;

public static class PropertyMediaMapping
{
    public static PropertyMediaDto ToDto(this PropertyMedia media, IBlobStorageService blobStorageService) =>
        new(
            media.Id,
            media.PropertyId,
            blobStorageService.GetPublicUrl(media.BlobName),
            media.ContentType,
            media.FileSizeBytes,
            media.ModerationStatus.ToString(),
            media.SortOrder,
            media.CreatedAt);
}
