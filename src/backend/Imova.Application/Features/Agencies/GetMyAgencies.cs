using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.Logos;
using Imova.Contracts.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies;

// The agencies the caller belongs to, with their role — deactivated ones too (members still see
// them). Oldest membership first.
public record GetMyAgenciesQuery(Guid UserId) : IRequest<List<MyAgencyDto>>;

public class GetMyAgenciesHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetMyAgenciesQuery, List<MyAgencyDto>>
{
    public async Task<List<MyAgencyDto>> Handle(GetMyAgenciesQuery request, CancellationToken cancellationToken)
    {
        var rows = await (
                from m in dbContext.AgencyMembers.AsNoTracking()
                join a in dbContext.Agencies.AsNoTracking() on m.AgencyId equals a.Id
                where m.UserId == request.UserId
                orderby m.JoinedAt
                select new { a.Id, a.Name, a.Slug, a.LogoBlobName, a.IsVerified, a.Status, m.Role })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new MyAgencyDto(
                r.Id,
                r.Name,
                r.Slug,
                r.LogoBlobName is null ? null : blobStorageService.GetPublicUrl(AgencyLogo.ThumbnailBlobName(r.LogoBlobName)),
                r.IsVerified,
                r.Status.ToString(),
                r.Role.ToString()))
            .ToList();
    }
}
