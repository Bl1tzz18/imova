using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Account;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Account.GetAccountDataSummary;

public class GetAccountDataSummaryHandler(IApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    : IRequestHandler<GetAccountDataSummaryQuery, AccountDataSummaryDto>
{
    public async Task<AccountDataSummaryDto> Handle(GetAccountDataSummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new AuthenticationFailedException("User not found.", ErrorCodes.UserNotFound);

        var publisherIds = await dbContext.Publishers.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        var statuses = await dbContext.Listings.AsNoTracking()
            .Where(l => publisherIds.Contains(l.PublisherId))
            .Select(l => l.Status)
            .ToListAsync(cancellationToken);

        return new AccountDataSummaryDto(
            statuses.Count,
            statuses.Count(s => s == ListingStatus.Active),
            await dbContext.Favorites.CountAsync(f => f.UserId == userId, cancellationToken),
            await dbContext.SavedSearches.CountAsync(s => s.UserId == userId, cancellationToken),
            await dbContext.Conversations.CountAsync(c => c.InitiatorUserId == userId || c.PublisherUserId == userId, cancellationToken),
            await dbContext.AgencyMembers.AnyAsync(m => m.UserId == userId, cancellationToken),
            await userManager.HasPasswordAsync(user));
    }
}
