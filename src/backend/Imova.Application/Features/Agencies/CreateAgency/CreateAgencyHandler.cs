using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.CreateAgency;

// Only a confirmed email may create an agency (403 agency.emailNotConfirmed), and an account owns at
// most AgencyOptions.MaxOwnedPerUser of them (400 agency.limitReached).
public class CreateAgencyHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    TimeProvider timeProvider,
    AgencyOptions options,
    IDatabaseErrors databaseErrors) : IRequestHandler<CreateAgencyCommand, AgencyDto>
{
    public const string SlugIndex = "IX_Agencies_Slug";

    public async Task<AgencyDto> Handle(CreateAgencyCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException("User not found.", ErrorCodes.UserNotFound);

        if (!user.EmailConfirmed)
        {
            throw new ForbiddenAccessException("Confirm your email address before creating an agency.", ErrorCodes.AgencyEmailNotConfirmed);
        }

        var owned = await dbContext.AgencyMembers.CountAsync(
            m => m.UserId == request.UserId && m.Role == AgencyRole.Owner, cancellationToken);
        if (owned >= options.MaxOwnedPerUser)
        {
            throw new ValidationException(
            [
                CodedFailure.Of(
                    nameof(CreateAgencyCommand.UserId),
                    $"An account can own at most {options.MaxOwnedPerUser} agencies.",
                    ErrorCodes.AgencyLimitReached,
                    CodedFailure.Params(("max", options.MaxOwnedPerUser))),
            ]);
        }

        var email = string.IsNullOrWhiteSpace(request.Email) ? user.Email ?? user.UserName ?? string.Empty : request.Email;
        var profile = await AgencyProfileValidator<CreateAgencyCommand>.ProfileAsync(dbContext, request, email, cancellationToken);

        var agency = await AddAsync(request, profile, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (databaseErrors.IsUniqueViolation(ex, SlugIndex))
        {
            // Someone took the same slug between our check and our save (two agencies created with
            // the same name at the same moment): forget the unsaved one, pick again — the pick now
            // sees theirs — and save once more.
            dbContext.AgencyMembers.RemoveRange(agency.Members.ToList());
            dbContext.Agencies.Remove(agency);
            agency = await AddAsync(request, profile, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await AgencyDtoLoader.LoadAsync(dbContext, blobStorageService, agency, request.UserId, isAdmin: false, cancellationToken);
    }

    // The agency and its Owner membership, saved together by the caller.
    private async Task<Agency> AddAsync(CreateAgencyCommand request, AgencyProfile profile, CancellationToken cancellationToken)
    {
        var slug = await AgencySlugs.FreeSlugAsync(dbContext, request.Name, agencyId: null, cancellationToken);
        var agency = Agency.Create(profile, slug, request.UserId, timeProvider.GetUtcNow());
        dbContext.Agencies.Add(agency);
        return agency;
    }
}
