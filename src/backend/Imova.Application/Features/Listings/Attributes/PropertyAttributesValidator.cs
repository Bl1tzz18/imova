using FluentValidation;
using FluentValidation.Results;
using Imova.Domain.Properties.Attributes;

namespace Imova.Application.Features.Listings.Attributes;

// Which TypeSpecificAttributes fields are required for a new write, per PropertyType. (Which
// fields are *allowed* at all is enforced earlier, by PropertyAttributesJson.TryParse rejecting
// any key outside the type's schema.) The frontend's listing form mirrors these rules in
// src/lib/property/attributeSchema.ts — keep the two in sync.
public static class PropertyAttributesValidator
{
    private static readonly ApartmentAttributesValidator Apartment = new();
    private static readonly HouseAttributesValidator House = new();
    private static readonly LandAttributesValidator Land = new();
    private static readonly CommercialAttributesValidator Commercial = new();
    private static readonly GarageAttributesValidator Garage = new();
    private static readonly RoomAttributesValidator Room = new();

    public static ValidationResult Validate(PropertyAttributes attributes) => attributes switch
    {
        ApartmentAttributes a => Apartment.Validate(a),
        HouseAttributes h => House.Validate(h),
        LandAttributes l => Land.Validate(l),
        CommercialAttributes c => Commercial.Validate(c),
        GarageAttributes g => Garage.Validate(g),
        RoomAttributes r => Room.Validate(r),
        _ => throw new ArgumentOutOfRangeException(nameof(attributes), attributes.GetType().Name, null),
    };
}

public class ApartmentAttributesValidator : AbstractValidator<ApartmentAttributes>
{
    public ApartmentAttributesValidator()
    {
        RuleFor(a => a.Rooms).NotNull().InclusiveBetween(1, 50);
        RuleFor(a => a.Floor).NotNull().InclusiveBetween(-5, 200);
        RuleFor(a => a.TotalFloors).NotNull().InclusiveBetween(1, 200);
        RuleFor(a => a.Floor)
            .LessThanOrEqualTo(a => a.TotalFloors!.Value)
            .WithMessage("Floor cannot be greater than TotalFloors.")
            .When(a => a.Floor.HasValue && a.TotalFloors.HasValue);
        RuleFor(a => a.Bathrooms).InclusiveBetween(0, 20);
        RuleFor(a => a.HeatingType).IsInEnum();
    }
}

public class HouseAttributesValidator : AbstractValidator<HouseAttributes>
{
    public HouseAttributesValidator()
    {
        RuleFor(h => h.Rooms).NotNull().InclusiveBetween(1, 100);
        RuleFor(h => h.HouseFloors).NotNull().InclusiveBetween(1, 10);
        RuleFor(h => h.LandAreaM2).GreaterThan(0);
        RuleFor(h => h.ConstructionType).IsInEnum();
    }
}

public class LandAttributesValidator : AbstractValidator<LandAttributes>
{
    public LandAttributesValidator()
    {
        RuleFor(l => l.LandDesignation).NotNull().IsInEnum();
        RuleFor(l => l.RoadAccess).IsInEnum();
    }
}

public class CommercialAttributesValidator : AbstractValidator<CommercialAttributes>
{
    public CommercialAttributesValidator()
    {
        RuleFor(c => c.SpaceType).NotNull().IsInEnum();
        RuleFor(c => c.Floor).InclusiveBetween(-5, 200);
        RuleFor(c => c.ElectricalPower).MaximumLength(50);
    }
}

public class GarageAttributesValidator : AbstractValidator<GarageAttributes>
{
    public GarageAttributesValidator()
    {
        RuleFor(g => g.GarageType).NotNull().IsInEnum();
    }
}

public class RoomAttributesValidator : AbstractValidator<RoomAttributes>
{
    public RoomAttributesValidator()
    {
        RuleFor(r => r.PrivateOrSharedBathroom).NotNull().IsInEnum();
        RuleFor(r => r.RoommateCount).InclusiveBetween(0, 20);
    }
}
