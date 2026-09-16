using FluentValidation;
using Imova.Domain.Properties;

namespace Imova.Application.Features.Properties.CreateProperty;

public class CreatePropertyValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyValidator()
    {
        RuleFor(c => c.OwnerId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.PropertyType).IsInEnum();
        RuleFor(c => c.ListingType).IsInEnum();
        RuleFor(c => c.Price).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.Country).NotEmpty().MaximumLength(100);
        RuleFor(c => c.City).NotEmpty().MaximumLength(100);
        RuleFor(c => c.District).MaximumLength(100);
        RuleFor(c => c.Latitude).InclusiveBetween(-90, 90);
        RuleFor(c => c.Longitude).InclusiveBetween(-180, 180);

        // Which of the fields below are required / must be omitted depends on PropertyType
        // (and, for PetsAllowed, ListingType) — see PropertyFieldRules.
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
