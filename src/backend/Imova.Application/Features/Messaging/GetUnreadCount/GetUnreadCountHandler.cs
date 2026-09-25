using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.GetUnreadCount;

public class GetUnreadCountHandler(IApplicationDbContext dbContext) : IRequestHandler<GetUnreadCountQuery, UnreadCountDto>
{
    public async Task<UnreadCountDto> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken) =>
        new(await MessagingAccess.UnreadCountAsync(dbContext, request.UserId, cancellationToken));
}
