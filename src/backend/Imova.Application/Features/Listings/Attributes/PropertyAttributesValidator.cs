using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using Imova.Domain.Properties.Attributes;

namespace Imova.Application.Features.Listings.Attributes;

// Which TypeSpecificAttributes fields are required for a new write, per PropertyType (and, for a
// House, which heating fields apply depending on its HeatingSystem). (Which
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

// The conditional heating-details rule shared by every type with a HeatingSystem: energy source
// and distribution are required for heating that has them (Heating.RequiresDetails) and must be
// empty otherwise.
internal static class HeatingRules
{
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, HeatingSystem?> heatingSystem,
        Expression<Func<T, HeatingSystem?>> heatingSystemField,
        Expression<Func<T, HeatingEnergySource?>> energySource,
        Expression<Func<T, HeatingDistribution?>> distribution)
    {
        validator.RuleFor(heatingSystemField).NotNull().IsInEnum();

        validator.RuleFor(energySource)
            .NotNull().WithMessage("HeatingEnergySource is required for this heating system.")
            .IsInEnum()
            .When(x => Heating.RequiresDetails(heatingSystem(x)));
        validator.RuleFor(energySource)
            .Null().WithMessage("HeatingEnergySource does not apply to this heating system.")
            .When(x => !Heating.RequiresDetails(heatingSystem(x)));

        validator.RuleFor(distribution)
            .NotNull().WithMessage("HeatingDistribution is required for this heating system.")
            .IsInEnum()
            .When(x => Heating.RequiresDetails(heatingSystem(x)));
        validator.RuleFor(distribution)
            .Null().WithMessage("HeatingDistribution does not apply to this heating system.")
            .When(x => !Heating.RequiresDetails(heatingSystem(x)));
    }
}

public class ApartmentAttributesValidator : AbstractValidator<ApartmentAttributes>
{
    public ApartmentAttributesValidator()
    {
        // Type & structure
        RuleFor(a => a.HousingStockType).NotNull().IsInEnum();
        RuleFor(a => a.BuildingMaterial).NotNull().IsInEnum();
        RuleFor(a => a.FinishCondition).NotNull().IsInEnum();
        RuleFor(a => a.Layout).NotNull().IsInEnum();
        RuleFor(a => a.Rooms).NotNull().InclusiveBetween(1, 50);
        RuleFor(a => a.Floor).NotNull().InclusiveBetween(-5, 200);
        RuleFor(a => a.TotalFloors).NotNull().InclusiveBetween(1, 200);
        RuleFor(a => a.Floor)
            .LessThanOrEqualTo(a => a.TotalFloors!.Value)
            .WithMessage("Floor cannot be greater than TotalFloors.")
            .When(a => a.Floor.HasValue && a.TotalFloors.HasValue);
        RuleFor(a => a.Bathrooms).InclusiveBetween(0, 20);

        // Areas
        RuleFor(a => a.LivingAreaM2).GreaterThan(0);
        RuleFor(a => a.KitchenAreaM2).GreaterThan(0);

        // Systems & utilities
        HeatingRules.Apply(this, a => a.HeatingSystem, a => a.HeatingSystem, a => a.HeatingEnergySource, a => a.HeatingDistribution);
        RuleFor(a => a.GasSupply).NotNull();

        // Finishing materials
        RuleFor(a => a.FloorMaterial).NotNull().IsInEnum();
    }
}

public class HouseAttributesValidator : AbstractValidator<HouseAttributes>
{
    public HouseAttributesValidator()
    {
        // Type & structure
        RuleFor(h => h.Rooms).NotNull().InclusiveBetween(1, 100);
        RuleFor(h => h.HouseType).NotNull().IsInEnum();
        RuleFor(h => h.BuildingMaterial).NotNull().IsInEnum();
        RuleFor(h => h.FinishCondition).NotNull().IsInEnum();
        RuleFor(h => h.HouseFloors).NotNull().InclusiveBetween(1, 10);
        RuleFor(h => h.CeilingHeightM).InclusiveBetween(1.5m, 10m);

        // Areas
        RuleFor(h => h.LivingAreaM2).NotNull().GreaterThan(0);
        RuleFor(h => h.LandAreaM2).NotNull().GreaterThan(0);
        RuleFor(h => h.KitchenAreaM2).GreaterThan(0);
        RuleFor(h => h.AtticAreaM2).GreaterThan(0);
        RuleFor(h => h.BasementAreaM2).GreaterThan(0);

        // Systems & utilities
        HeatingRules.Apply(this, h => h.HeatingSystem, h => h.HeatingSystem, h => h.HeatingEnergySource, h => h.HeatingDistribution);
        RuleFor(h => h.WaterSupply).NotNull().IsInEnum();
        RuleFor(h => h.Sewerage).NotNull().IsInEnum();
        RuleFor(h => h.GasSupply).NotNull();

        // Finishing materials
        RuleFor(h => h.FloorMaterial).NotNull().IsInEnum();
        RuleFor(h => h.AtticMaterial).IsInEnum();
        RuleFor(h => h.RoofMaterial).NotNull().IsInEnum();
        RuleFor(h => h.WindowType).NotNull().IsInEnum();
    }
}

public class LandAttributesValidator : AbstractValidator<LandAttributes>
{
    public LandAttributesValidator()
    {
        // Type & area
        RuleFor(l => l.PlotType).NotNull().IsInEnum();
        RuleFor(l => l.LocationContext).NotNull().IsInEnum();
        RuleFor(l => l.SoilQualityScore)
            .InclusiveBetween(1, 100)
            .When(l => LandAttributes.AllowsSoilQuality(l.PlotType));
        RuleFor(l => l.SoilQualityScore)
            .Null().WithMessage("SoilQualityScore only applies to agricultural land.")
            .When(l => !LandAttributes.AllowsSoilQuality(l.PlotType));

        // Utilities & access
        RuleFor(l => l.RoadAccess).NotNull().IsInEnum();
        RuleFor(l => l.GasPipelineAtBoundary).NotNull();
        RuleFor(l => l.ElectricitySupplyAtBoundary).NotNull();
        RuleFor(l => l.SewerageAtBoundary).NotNull();
        RuleFor(l => l.IrrigationSystem).NotNull();
        RuleFor(l => l.PhoneLineAvailable).NotNull();
    }
}

public class CommercialAttributesValidator : AbstractValidator<CommercialAttributes>
{
    public CommercialAttributesValidator()
    {
        // Type & structure
        RuleFor(c => c.SpaceType).NotNull().IsInEnum();
        RuleFor(c => c.FinishCondition).NotNull().IsInEnum();
        RuleFor(c => c.Floor).NotNull().InclusiveBetween(-5, 200);
        RuleFor(c => c.TotalFloorsInBuilding).InclusiveBetween(1, 200);
        RuleFor(c => c.Floor)
            .LessThanOrEqualTo(c => c.TotalFloorsInBuilding!.Value)
            .WithMessage("Floor cannot be greater than TotalFloorsInBuilding.")
            .When(c => c.Floor.HasValue && c.TotalFloorsInBuilding.HasValue);

        // Areas
        RuleFor(c => c.WorkingAreaM2).GreaterThan(0);
        RuleFor(c => c.NumberOfOffices)
            .InclusiveBetween(1, 500)
            .When(c => CommercialAttributes.AllowsNumberOfOffices(c.SpaceType));
        RuleFor(c => c.NumberOfOffices)
            .Null().WithMessage("NumberOfOffices only applies to office space.")
            .When(c => !CommercialAttributes.AllowsNumberOfOffices(c.SpaceType));

        // Systems & utilities
        RuleFor(c => c.Bathrooms).NotNull().InclusiveBetween(0, 50);
        RuleFor(c => c.PhoneLinesCount).InclusiveBetween(0, 100);
        RuleFor(c => c.MainStreetAccess).NotNull();
        RuleFor(c => c.ElectricalPower).MaximumLength(50);
        RuleFor(c => c.GasSupply).NotNull();
    }
}

public class GarageAttributesValidator : AbstractValidator<GarageAttributes>
{
    public GarageAttributesValidator()
    {
        RuleFor(g => g.ParkingType).NotNull().IsInEnum();
    }
}

public class RoomAttributesValidator : AbstractValidator<RoomAttributes>
{
    public RoomAttributesValidator()
    {
        RuleFor(r => r.BathroomType).NotNull().IsInEnum();
        RuleFor(r => r.RoommateCount).InclusiveBetween(0, 20);
    }
}
