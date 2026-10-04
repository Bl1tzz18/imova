using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Account;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Account.EmailPreferences;

// The "Notificări" tab of /account: the optional emails the user can turn on or off.
public record GetEmailPreferencesQuery(Guid UserId) : IRequest<EmailPreferencesDto>;

public record UpdateEmailPreferencesCommand(Guid UserId, bool FavoriteUpdates) : IRequest<EmailPreferencesDto>;

public class GetEmailPreferencesHandler(IApplicationDbContext dbContext) : IRequestHandler<GetEmailPreferencesQuery, EmailPreferencesDto>
{
    public async Task<EmailPreferencesDto> Handle(GetEmailPreferencesQuery request, CancellationToken cancellationToken) =>
        await dbContext.Users.AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new EmailPreferencesDto(u.EmailFavoriteUpdates))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new AuthenticationFailedException("The account no longer exists.");
}

public class UpdateEmailPreferencesHandler(IApplicationDbContext dbContext) : IRequestHandler<UpdateEmailPreferencesCommand, EmailPreferencesDto>
{
    public async Task<EmailPreferencesDto> Handle(UpdateEmailPreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
                   ?? throw new AuthenticationFailedException("The account no longer exists.");
        user.EmailFavoriteUpdates = request.FavoriteUpdates;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new EmailPreferencesDto(user.EmailFavoriteUpdates);
    }
}
