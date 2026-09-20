using FluentValidation;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Properties;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.UpdateProperty;

// Mirrors CreatePropertyValidator's rules (including the PropertyType/ListingType-driven field
// requirements from PropertyFieldRules) — an edit has to satisfy the exact same invariants a
// fresh listing would, since PropertyType/ListingType are themselves editable here.
public class UpdatePropertyValidator : AbstractValidator<UpdatePropertyCommand>
{
    public UpdatePropertyValidator(IApplicationDbContext dbContext)
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.PropertyType).IsInEnum();
        RuleFor(c => c.ListingType).IsInEnum();
        RuleFor(c => c.Price).GreaterThan(0);
        // Only these three currencies are offered on the frontend dropdown — the backend rejects
        // anything else rather than silently accepting it from a direct API call.
        RuleFor(c => c.Currency)
            .Must(SupportedCurrencies.All.Contains)
            .WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies.All)}.");
        RuleFor(c => c.Country).NotEmpty().MaximumLength(100);

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

        RuleFor(c => c.StreetAddress).MaximumLength(200);

        RuleFor(c => c.Area)
            .NotNull().WithMessage("Area is required for this property type.")
            .When(c => PropertyFieldRules.Area(c.PropertyType) == FieldRequirement.Required);
        RuleFor(c => c.Area)
            .Null().WithMessage("Area does not apply to this property type.")
            .When(c => PropertyFieldRules.Area(c.PropertyType) == FieldRequirement.Hidden);
        RuleFor(c => c.Area)
            .GreaterThan(0)
            .When(c => c.Area.HasValue);

        RuleFor(c => c.Rooms)
            .NotNull().WithMessage("Rooms is required for this property type.")
            .When(c => PropertyFieldRules.Rooms(c.PropertyType) == FieldRequirement.Required);
        RuleFor(c => c.Rooms)
            .Null().WithMessage("Rooms does not apply to this property type.")
            .When(c => PropertyFieldRules.Rooms(c.PropertyType) == FieldRequirement.Hidden);
        RuleFor(c => c.Rooms)
            .GreaterThan(0)
            .When(c => c.Rooms.HasValue);

        RuleFor(c => c.Bathrooms)
            .Null().WithMessage("Bathrooms does not apply to this property type.")
            .When(c => PropertyFieldRules.Bathrooms(c.PropertyType) == FieldRequirement.Hidden);
        RuleFor(c => c.Bathrooms)
            .GreaterThanOrEqualTo((short)0)
            .When(c => c.Bathrooms.HasValue);

        RuleFor(c => c.Floor)
            .NotNull().WithMessage("Floor is required for this property type.")
            .When(c => PropertyFieldRules.Floor(c.PropertyType) == FieldRequirement.Required);
        RuleFor(c => c.Floor)
            .Null().WithMessage("Floor does not apply to this property type.")
            .When(c => PropertyFieldRules.Floor(c.PropertyType) == FieldRequirement.Hidden);
        RuleFor(c => c.Floor)
            .InclusiveBetween((short)-5, (short)200)
            .When(c => c.Floor.HasValue);
        RuleFor(c => c.Floor)
            .LessThanOrEqualTo(c => c.TotalFloors!.Value)
            .WithMessage("Floor cannot be greater than TotalFloors.")
            .When(c => c.Floor.HasValue && c.TotalFloors.HasValue);

        RuleFor(c => c.TotalFloors)
            .NotNull().WithMessage("TotalFloors is required for this property type.")
            .When(c => PropertyFieldRules.TotalFloors(c.PropertyType) == FieldRequirement.Required);
        RuleFor(c => c.TotalFloors)
            .Null().WithMessage("TotalFloors does not apply to this property type.")
            .When(c => PropertyFieldRules.TotalFloors(c.PropertyType) == FieldRequirement.Hidden);
        RuleFor(c => c.TotalFloors)
            .GreaterThan((short)0)
            .When(c => c.TotalFloors.HasValue);

        RuleFor(c => c.YearBuilt)
            .Null().WithMessage("YearBuilt does not apply to this property type.")
            .When(c => PropertyFieldRules.YearBuilt(c.PropertyType) == FieldRequirement.Hidden);
        RuleFor(c => c.YearBuilt)
            .InclusiveBetween((short)1800, (short)(DateTime.UtcNow.Year + 1))
            .When(c => c.YearBuilt.HasValue);

        RuleFor(c => c.Furnished)
            .Null().WithMessage("Furnished does not apply to this property type.")
            .When(c => PropertyFieldRules.Furnished(c.PropertyType) == FieldRequirement.Hidden);

        RuleFor(c => c.ParkingAvailable)
            .Null().WithMessage("ParkingAvailable does not apply to this property type.")
            .When(c => PropertyFieldRules.ParkingAvailable(c.PropertyType) == FieldRequirement.Hidden);

        RuleFor(c => c.PetsAllowed)
            .Null().WithMessage("PetsAllowed does not apply to this property type or listing type.")
            .When(c => PropertyFieldRules.PetsAllowed(c.PropertyType, c.ListingType) == FieldRequirement.Hidden);
    }
}
