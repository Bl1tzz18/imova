using Imova.Application.Common.Interfaces;
using Imova.Contracts.Publishers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Publishers.GetMyPublishers;

public class GetMyPublishersHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMyPublishersQuery, List<PublisherDto>>
{
    public async Task<List<PublisherDto>> Handle(GetMyPublishersQuery request, CancellationToken cancellationToken)
    {
        await PublisherProvisioning.EnsureIndividualAsync(dbContext, request.UserId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var publishers = await dbContext.Publishers
            .AsNoTracking()
            .Where(p => p.UserId == request.UserId)
            .OrderBy(p => p.PublisherType)
            .ToListAsync(cancellationToken);

        return publishers.Select(p => p.ToDto(includeContactDetails: true)).ToList();
    }
}
