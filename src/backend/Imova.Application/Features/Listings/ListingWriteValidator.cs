using FluentValidation;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings.Attributes;
using Imova.Domain.Amenities;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings;

// Rules shared by CreateListingValidator and UpdateListingValidator — an edit has to satisfy the
// exact same invariants a fresh listing would, since every field is editable.
public abstract class ListingWriteValidator<T> : AbstractValidator<T>
    where T : IListingWriteCommand
{
    public const int MaxAmenities = 50;
    public const int MaxProximities = 20;

    protected ListingWriteValidator(IApplicationDbContext dbContext)
    {
        // --- Property ---
        RuleFor(c => c.PropertyType).IsInEnum();
        RuleFor(c => c.TotalAreaM2).GreaterThan(0).LessThan(100_000_000);
        RuleFor(c => c.YearBuilt)
            .Null().WithMessage("YearBuilt does not apply to Land.")
            .When(c => c.PropertyType == PropertyType.Land);
        RuleFor(c => c.YearBuilt).InclusiveBetween(1800, DateTime.UtcNow.Year + 1);
        RuleFor(c => c.Condition)
            .Null().WithMessage("Condition does not apply to Land.")
            .When(c => c.PropertyType == PropertyType.Land);
        // House, Apartment and Commercial describe their state with the more granular
        // typeSpecificAttributes.finishCondition; only Garage and Room use the general Condition.
        RuleFor(c => c.Condition)
            .Null().WithMessage("Condition does not apply to this property type — use TypeSpecificAttributes.finishCondition.")
            .When(c => c.PropertyType is PropertyType.House or PropertyType.Apartment or PropertyType.Commercial);
        RuleFor(c => c.Condition).IsInEnum();

        // Parse against the schema PropertyType selects (which rejects any field belonging to
        // another type), then apply that type's own required-field rules.
        RuleFor(c => c.TypeSpecificAttributes)
            .Custom((json, context) =>
            {
                var command = context.InstanceToValidate;
                if (!PropertyAttributesJson.TryParse(command.PropertyType, json, out var attributes, out var error))
                {
                    context.AddFailure(nameof(IListingWriteCommand.TypeSpecificAttributes), error!);
                    return;
                }

                foreach (var failure in PropertyAttributesValidator.Validate(attributes!).Errors)
                {
                    context.AddFailure(
                        $"{nameof(IListingWriteCommand.TypeSpecificAttributes)}.{failure.PropertyName}",
                        failure.ErrorMessage);
                }
            })
            .When(c => Enum.IsDefined(c.PropertyType));

        RuleFor(c => c.AmenityIds)
            .Must(ids => ids!.Count <= MaxAmenities)
            .WithMessage($"At most {MaxAmenities} amenities can be selected.")
            .MustAsync(async (ids, cancellationToken) =>
            {
                var distinct = ids!.Distinct().ToList();
                var known = await dbContext.Amenities.CountAsync(a => distinct.Contains(a.Id), cancellationToken);
                return known == distinct.Count;
            })
            .WithMessage("AmenityIds contains an unknown amenity.")
            .CustomAsync(async (ids, context, cancellationToken) =>
            {
                var command = context.InstanceToValidate;
                var amenities = await dbContext.Amenities
                    .Where(a => ids!.Contains(a.Id))
                    .ToListAsync(cancellationToken);

                foreach (var amenity in amenities.Where(a => !a.AppliesTo(command.PropertyType)))
                {
                    context.AddFailure(
                        nameof(IListingWriteCommand.AmenityIds),
                        $"The '{amenity.Key}' amenity doesn't apply to a {command.PropertyType}.");
                }
            })
            .When(c => c.AmenityIds is { Count: > 0 });

        RuleFor(c => c.ProximityIds)
            .Must(ids => ids!.Count <= MaxProximities)
            .WithMessage($"At most {MaxProximities} proximities can be selected.")
            .MustAsync(async (ids, cancellationToken) =>
            {
                var distinct = ids!.Distinct().ToList();
                var known = await dbContext.Proximities.CountAsync(p => distinct.Contains(p.Id), cancellationToken);
                return known == distinct.Count;
            })
            .WithMessage("ProximityIds contains an unknown proximity.")
            .CustomAsync(async (ids, context, cancellationToken) =>
            {
                var command = context.InstanceToValidate;
                var proximities = await dbContext.Proximities
                    .Where(p => ids!.Contains(p.Id))
                    .ToListAsync(cancellationToken);

                foreach (var proximity in proximities.Where(p => !p.AppliesTo(command.PropertyType)))
                {
                    context.AddFailure(
                        nameof(IListingWriteCommand.ProximityIds),
                        $"The '{proximity.Key}' proximity doesn't apply to a {command.PropertyType}.");
                }
            })
            .When(c => c.ProximityIds is { Count: > 0 });

        RuleFor(c => c.Country).NotEmpty().MaximumLength(100);

        // RaionId/LocalitateId reference the CUATM-seeded reference tables; ChisinauSectorId the
        // informal Chișinău neighborhood table (only meaningful when the raion is Chișinău).
        RuleFor(c => c.RaionId)
            .MustAsync((raionId, cancellationToken) =>
                dbContext.Raioane.AnyAsync(r => r.Id == raionId, cancellationToken))
            .WithMessage("RaionId does not reference a known raion.");
        RuleFor(c => c.LocalitateId)
            .MustAsync((command, localitateId, cancellationToken) =>
                dbContext.Localitati.AnyAsync(
                    l => l.Id == localitateId!.Value && l.RaionId == command.RaionId,
                    cancellationToken))
            .WithMessage("LocalitateId does not reference a known localitate belonging to the selected raion.")
            .When(c => c.LocalitateId.HasValue);
        RuleFor(c => c.ChisinauSectorId)
            .MustAsync((chisinauSectorId, cancellationToken) =>
                dbContext.ChisinauSectors.AnyAsync(s => s.Id == chisinauSectorId!.Value, cancellationToken))
            .WithMessage("ChisinauSectorId does not reference a known sector.")
            .When(c => c.ChisinauSectorId.HasValue);
        RuleFor(c => c.RaionId)
            .MustAsync((raionId, cancellationToken) =>
                dbContext.Raioane.AnyAsync(r => r.Id == raionId && r.LocalityLabel == LocalityLabel.Sector, cancellationToken))
            .WithMessage("ChisinauSectorId can only be set when the selected raion is Chișinău.")
            .When(c => c.ChisinauSectorId.HasValue)
            .OverridePropertyName(nameof(IListingWriteCommand.ChisinauSectorId));

        // A listing can be in a suburb or an informal Chișinău neighborhood, never both at once
        // (they're physically different places). Neither being set stays allowed.
        RuleFor(c => c)
            .Must(c => c.LocalitateId is null || c.ChisinauSectorId is null)
            .WithMessage("LocalitateId and ChisinauSectorId cannot both be set — pick a suburb or a sector, not both.")
            .OverridePropertyName(nameof(IListingWriteCommand.ChisinauSectorId));

        RuleFor(c => c.StreetAddress)
            .NotEmpty().WithMessage("Street address is required.")
            .MaximumLength(200);
        RuleFor(c => c.BuildingNumber).MaximumLength(20);

        // --- Listing ---
        RuleFor(c => c.TransactionType).IsInEnum();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.Price).GreaterThan(0).LessThan(10_000_000_000m);
        RuleFor(c => c.Currency)
            .IsInEnum()
            .WithMessage($"Currency must be one of: {string.Join(", ", Enum.GetNames<Currency>())}.");

        RuleFor(c => c.RentalDetails)
            .Null().WithMessage("RentalDetails only applies to a rental listing.")
            .When(c => c.TransactionType == TransactionType.Sale);
        RuleFor(c => c.RentalDetails!.MinLeasePeriodMonths)
            .InclusiveBetween(1, 120)
            .OverridePropertyName("RentalDetails.MinLeasePeriodMonths")
            .When(c => c.RentalDetails is not null);
        RuleFor(c => c.RentalDetails!.SecurityDepositAmount)
            .GreaterThanOrEqualTo(0).LessThan(10_000_000_000m)
            .OverridePropertyName("RentalDetails.SecurityDepositAmount")
            .When(c => c.RentalDetails is not null);

        // Pets are a required Yes/No for a rented home and not asked for anything else.
        RuleFor(c => c.RentalDetails)
            .Must(details => details?.PetsAllowed is not null)
            .WithMessage("RentalDetails.PetsAllowed is required for this property type.")
            .OverridePropertyName("RentalDetails.PetsAllowed")
            .When(c => c.TransactionType == TransactionType.Rent && Imova.Domain.Listings.RentalDetails.PetsApplyTo(c.PropertyType));
        RuleFor(c => c.RentalDetails)
            .Must(details => details?.PetsAllowed is null)
            .WithMessage("RentalDetails.PetsAllowed does not apply to this property type.")
            .OverridePropertyName("RentalDetails.PetsAllowed")
            .When(c => c.TransactionType == TransactionType.Rent && !Imova.Domain.Listings.RentalDetails.PetsApplyTo(c.PropertyType));
    }
}
