using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using Imova.Domain.Listings;
using Imova.Domain.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Messaging.StartConversation;

public class StartConversationHandler(
    IApplicationDbContext dbContext,
    MessageDelivery messageDelivery,
    MessagingOptions options,
    TimeProvider timeProvider)
    : IRequestHandler<StartConversationCommand, StartConversationResultDto?>
{
    public async Task<StartConversationResultDto?> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.ListingId && l.Status == ListingStatus.Active, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        // Always the account behind the listing's Publisher — even when the listing names another
        // contact person (ContactPersonType.Other), who has no account to receive messages.
        var publisherUserId = await dbContext.Publishers
            .Where(p => p.Id == listing.PublisherId)
            .Select(p => p.UserId)
            .FirstAsync(cancellationToken);
        if (publisherUserId == request.UserId)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.ListingId), "You can't send a message about your own listing.")]);
        }

        var conversation = await dbContext.Conversations
            .FirstOrDefaultAsync(c => c.ListingId == listing.Id && c.InitiatorUserId == request.UserId, cancellationToken);
        var reused = conversation is not null;

        if (conversation is null)
        {
            var now = timeProvider.GetUtcNow();
            var startedLastHour = await dbContext.Conversations.CountAsync(
                c => c.InitiatorUserId == request.UserId && c.CreatedAt > now.AddHours(-1), cancellationToken);
            if (startedLastHour >= options.MaxNewConversationsPerHour)
            {
                throw new TooManyRequestsException(
                    $"You can start at most {options.MaxNewConversationsPerHour} new conversations per hour. Please try again later.");
            }

            conversation = Conversation.Start(listing.Id, request.UserId, publisherUserId, now);
            dbContext.Conversations.Add(conversation);
        }

        var message = await messageDelivery.SendAsync(
            conversation, request.UserId, request.Body, request.AttachmentBlobNames, cancellationToken);
        return new StartConversationResultDto(conversation.Id, message, reused);
    }
}
