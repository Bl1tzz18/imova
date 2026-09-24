using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Publishers;
using Imova.Domain.Publishers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Publishers.CreateAgencyPublisher;

public class CreateAgencyPublisherHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateAgencyPublisherCommand, PublisherDto>
{
    public async Task<PublisherDto> Handle(CreateAgencyPublisherCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException("User not found.");

        // One agency per user (also enforced by a unique index) — surfaced as a validation error
        // rather than a raw constraint violation.
        var alreadyHasAgency = await dbContext.Publishers.AnyAsync(
            p => p.UserId == request.UserId && p.PublisherType == PublisherType.Agency, cancellationToken);
        if (alreadyHasAgency)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(CreateAgencyPublisherCommand.UserId), "This account already has an agency publisher.")]);
        }

        var publisher = Publisher.CreateAgency(
            request.UserId,
            request.DisplayName.Trim(),
            request.Phone.Trim(),
            string.IsNullOrWhiteSpace(request.Email) ? user.Email ?? user.UserName ?? string.Empty : request.Email.Trim(),
            string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim(),
            string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim());

        dbContext.Publishers.Add(publisher);
        await dbContext.SaveChangesAsync(cancellationToken);

        return publisher.ToDto(includeContactDetails: true);
    }
}
