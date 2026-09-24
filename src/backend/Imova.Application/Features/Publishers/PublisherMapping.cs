using Imova.Contracts.Publishers;
using Imova.Domain.Publishers;

namespace Imova.Application.Features.Publishers;

public static class PublisherMapping
{
    public static PublisherDto ToDto(this Publisher publisher, bool includeContactDetails) =>
        new(
            publisher.Id,
            publisher.UserId,
            publisher.PublisherType.ToString(),
            publisher.DisplayName,
            includeContactDetails ? publisher.Phone : null,
            includeContactDetails ? publisher.Email : null,
            publisher.LogoUrl,
            publisher.Bio);
}
