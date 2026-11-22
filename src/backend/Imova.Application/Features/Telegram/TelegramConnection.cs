using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Account;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Telegram;

// The account side of connecting Telegram: whether it's connected, a fresh "Conectează Telegram"
// link, and "Deconectează". The chat side (pressing Start, /stop) is TelegramBotUpdates.
public record GetTelegramConnectionQuery(Guid UserId) : IRequest<TelegramConnectionDto>;

public record StartTelegramLinkCommand(Guid UserId) : IRequest<TelegramLinkStartDto>;

public record DisconnectTelegramCommand(Guid UserId) : IRequest;

public class GetTelegramConnectionHandler(IApplicationDbContext dbContext, TelegramOptions options)
    : IRequestHandler<GetTelegramConnectionQuery, TelegramConnectionDto>
{
    public async Task<TelegramConnectionDto> Handle(GetTelegramConnectionQuery request, CancellationToken cancellationToken)
    {
        var link = await dbContext.TelegramLinks.AsNoTracking().FirstOrDefaultAsync(l => l.UserId == request.UserId, cancellationToken);
        return new TelegramConnectionDto(
            options.IsAvailable, link is not null, link?.TelegramUsername, link?.TelegramFirstName, link?.LinkedAt);
    }
}

public class StartTelegramLinkHandler(IApplicationDbContext dbContext, TelegramOptions options, TimeProvider timeProvider)
    : IRequestHandler<StartTelegramLinkCommand, TelegramLinkStartDto>
{
    public async Task<TelegramLinkStartDto> Handle(StartTelegramLinkCommand request, CancellationToken cancellationToken)
    {
        if (!options.IsAvailable)
        {
            throw new ConflictException("Telegram isn't set up on this site.", ErrorCodes.TelegramUnavailable);
        }

        if (!await dbContext.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken))
        {
            throw new AuthenticationFailedException("The account no longer exists.");
        }

        // Only the newest link works — and this also clears the account's expired, never-used codes.
        dbContext.TelegramLinkCodes.RemoveRange(
            await dbContext.TelegramLinkCodes.Where(c => c.UserId == request.UserId).ToListAsync(cancellationToken));

        var now = timeProvider.GetUtcNow();
        var code = TelegramLinkCodes.NewCode();
        var expiresAt = now + TelegramLinkCodes.Lifetime;
        dbContext.TelegramLinkCodes.Add(new TelegramLinkCode
        {
            Id = Guid.NewGuid(), UserId = request.UserId, CodeHash = TelegramLinkCodes.Hash(code), CreatedAt = now, ExpiresAt = expiresAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TelegramLinkStartDto(options.StartUrl(code), expiresAt);
    }
}

// Idempotent: nothing connected → nothing to do. The chat isn't told (the API has no bot token);
// the bot just stops writing there, and /start in it says the chat isn't connected.
public class DisconnectTelegramHandler(IApplicationDbContext dbContext) : IRequestHandler<DisconnectTelegramCommand>
{
    public async Task Handle(DisconnectTelegramCommand request, CancellationToken cancellationToken)
    {
        var link = await dbContext.TelegramLinks.FirstOrDefaultAsync(l => l.UserId == request.UserId, cancellationToken);
        if (link is null)
        {
            return;
        }

        dbContext.TelegramLinks.Remove(link);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
