using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Domain.Agencies;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies;

// The editable details, shared by CreateAgencyCommand and UpdateAgencyCommand.
public interface IAgencyProfileCommand
{
    string Name { get; }

    string Phone { get; }

    // Optional on create (the account's email is used); required on update.
    string? Email { get; }

    string? Bio { get; }

    string? Website { get; }

    string? Address { get; }

    Guid? RaionId { get; }
}

public abstract class AgencyProfileValidator<T> : AbstractValidator<T>
    where T : IAgencyProfileCommand
{
    protected AgencyProfileValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(Agency.MaxNameLength);
        RuleFor(c => c.Phone).Cascade(CascadeMode.Stop).ValidPhoneNumber();
        RuleFor(c => c.Email).EmailAddress().MaximumLength(Agency.MaxEmailLength)
            .When(c => !string.IsNullOrWhiteSpace(c.Email));
        RuleFor(c => c.Bio).MaximumLength(Agency.MaxBioLength);
        RuleFor(c => c.Address).MaximumLength(Agency.MaxAddressLength);
        RuleFor(c => c.Website).Cascade(CascadeMode.Stop)
            .MaximumLength(Agency.MaxWebsiteLength)
            .Must(BeAWebAddress)
            .WithMessage("Website must be an http(s) address, e.g. https://agentia.md.")
            .WithErrorCode(ErrorCodes.AgencyWebsiteInvalid)
            .When(c => !string.IsNullOrWhiteSpace(c.Website));
    }

    // The command as the domain's profile, once the city is known to exist (else a 400 on RaionId).
    public static async Task<AgencyProfile> ProfileAsync(
        IApplicationDbContext dbContext, T command, string email, CancellationToken cancellationToken)
    {
        if (command.RaionId is { } raionId && raionId != Guid.Empty
            && !await dbContext.Raioane.AnyAsync(r => r.Id == raionId, cancellationToken))
        {
            throw new ValidationException(
                [CodedFailure.Of(nameof(IAgencyProfileCommand.RaionId), "RaionId does not reference a known raion.", ErrorCodes.AgencyRaionUnknown)]);
        }

        return new AgencyProfile(command.Name, command.Phone, email, command.Bio, command.Website, command.Address, command.RaionId);
    }

    private static bool BeAWebAddress(string? website) =>
        Uri.TryCreate(website?.Trim(), UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https"
        && uri.Host.Contains('.');
}
