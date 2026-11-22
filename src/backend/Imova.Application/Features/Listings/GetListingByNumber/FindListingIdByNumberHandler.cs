using Imova.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.GetListingByNumber;

public class FindListingIdByNumberHandler(IApplicationDbContext dbContext) : IRequestHandler<FindListingIdByNumberQuery, Guid?>
{
    public async Task<Guid?> Handle(FindListingIdByNumberQuery request, CancellationToken cancellationToken) =>
        await dbContext.Listings
            .AsNoTracking()
            .Where(l => l.Number == request.Number)
            .Select(l => (Guid?)l.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
