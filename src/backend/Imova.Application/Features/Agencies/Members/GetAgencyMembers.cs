using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Members;

// The agency's members — for its members and site admins only. Null (404) when the caller may not
// see the agency at all; 403 for anyone else signed in. Owners first, then Admins, then Agents,
// longest-standing first.
public record GetAgencyMembersQuery(Guid AgencyId, Guid UserId, bool IsAdmin) : IRequest<List<AgencyMemberDto>?>;

public class GetAgencyMembersHandler(IApplicationDbContext dbContext) : IRequestHandler<GetAgencyMembersQuery, List<AgencyMemberDto>?>
{
    public async Task<List<AgencyMemberDto>?> Handle(GetAgencyMembersQuery request, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.AsNoTracking().Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.CanView(agency, request.UserId, request.IsAdmin))
        {
            return null;
        }

        if (!request.IsAdmin && !agency.IsMember(request.UserId))
        {
            throw new ForbiddenAccessException();
        }

        var userIds = agency.Members.Select(m => m.UserId).ToList();
        var users = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email, u.ProfilePictureUrl })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var listingCounts = await (
                from l in dbContext.Listings.AsNoTracking()
                join p in dbContext.Publishers.AsNoTracking() on l.PublisherId equals p.Id
                where l.AgencyId == agency.Id
                group l by p.UserId into g
                select new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        return agency.Members
            .OrderBy(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .Select(m =>
            {
                var user = users.GetValueOrDefault(m.UserId);
                var email = user?.Email ?? string.Empty;
                var name = !string.IsNullOrWhiteSpace(user?.DisplayName) ? user.DisplayName : email;
                return new AgencyMemberDto(
                    m.UserId, name, email, user?.ProfilePictureUrl, m.Role.ToString(), m.JoinedAt, listingCounts.GetValueOrDefault(m.UserId));
            })
            .ToList();
    }
}
