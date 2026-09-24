// Mirrors the backend's per-PropertyType TypeSpecificAttributes schemas
// (Imova.Domain.Properties.Attributes) and which of their fields are required
// (Imova.Application.Features.Listings.Attributes.PropertyAttributesValidator) — the backend is
// the source of truth and validates independently; this only drives the form. Keep them in sync.

export const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;
export type PropertyTypeName = (typeof PROPERTY_TYPES)[number];

// A field that only exists while another field has one of the given values (e.g. a House's heating
// energy source, only for boiler/heat-pump/solar heating). While hidden it's never submitted, and
// `required` applies only while it's visible.
export type Visibility = { field: string; values: readonly string[] };

type FieldBase = { name: string; visibleWhen?: Visibility };

export type AttributeField = FieldBase &
  (
    | { kind: "int"; required?: boolean; min: number; max: number }
    | { kind: "decimal"; required?: boolean; min: number; max?: number }
    | { kind: "enum"; required?: boolean; options: readonly string[] }
    | { kind: "bool" }
    | { kind: "text"; maxLength: number }
    // A nested object of booleans, e.g. utilitiesAtBoundary: { water, electricity, gas }.
    | { kind: "flags"; flags: readonly string[] }
  );

// Mirrors HouseAttributes.RequiresHeatingDetails on the backend.
export const HEATING_WITH_OWN_SOURCE = ["OwnBoiler", "HeatPump", "SolarPanels"] as const;
const heatingDetailsVisibility: Visibility = { field: "heatingSystem", values: HEATING_WITH_OWN_SOURCE };

export const ATTRIBUTE_SCHEMA: Record<PropertyTypeName, readonly AttributeField[]> = {
  Apartment: [
    { name: "rooms", kind: "int", required: true, min: 1, max: 50 },
    { name: "floor", kind: "int", required: true, min: -5, max: 200 },
    { name: "totalFloors", kind: "int", required: true, min: 1, max: 200 },
    { name: "bathrooms", kind: "int", min: 0, max: 20 },
    { name: "heatingType", kind: "enum", options: ["Centralized", "Autonomous", "Other"] },
  ],
  House: [
    // Type & structure
    { name: "houseType", kind: "enum", required: true, options: ["Individual", "Duplex", "Triplex", "Townhouse", "Villa", "Other"] },
    {
      name: "buildingMaterial",
      kind: "enum",
      required: true,
      options: [
        "Brick", "Panel", "ConcreteBlock", "LimestoneBlock", "Monolith", "Concrete", "Combined", "AeratedConcrete",
        "Wood", "Adobe", "Other",
      ],
    },
    {
      name: "houseCondition",
      kind: "enum",
      required: true,
      options: [
        "ToBeDemolished", "IndividualDesign", "GrayStructure", "EuroRenovated", "WhiteStructure", "NeedsRepair",
        "Unfinished", "CosmeticRepair", "NoRepair",
      ],
    },
    { name: "houseFloors", kind: "int", required: true, min: 1, max: 10 },
    { name: "rooms", kind: "int", required: true, min: 1, max: 100 },
    { name: "ceilingHeightM", kind: "decimal", min: 1.5, max: 10 },
    // Areas
    { name: "livingAreaM2", kind: "decimal", required: true, min: 0.01 },
    { name: "landAreaM2", kind: "decimal", required: true, min: 0.01 },
    { name: "kitchenAreaM2", kind: "decimal", min: 0.01 },
    { name: "atticAreaM2", kind: "decimal", min: 0.01 },
    { name: "basementAreaM2", kind: "decimal", min: 0.01 },
    // Systems & utilities
    {
      name: "heatingSystem",
      kind: "enum",
      required: true,
      options: ["OwnBoiler", "Convector", "InfraredPanels", "DistrictHeating", "HeatPump", "SolarPanels", "Stove", "None"],
    },
    {
      name: "heatingEnergySource",
      kind: "enum",
      required: true,
      options: ["Gas", "Electricity", "Wood", "Combined"],
      visibleWhen: heatingDetailsVisibility,
    },
    {
      name: "heatingDistribution",
      kind: "enum",
      required: true,
      options: ["Radiators", "UnderfloorHeating", "Air"],
      visibleWhen: heatingDetailsVisibility,
    },
    { name: "waterSupply", kind: "enum", required: true, options: ["CentralNetwork", "Well", "DrilledWell", "Cistern", "None"] },
    { name: "sewerage", kind: "enum", required: true, options: ["Central", "SepticTank", "None"] },
    { name: "gasSupply", kind: "bool" },
    // Finishing materials
    { name: "floorMaterial", kind: "enum", required: true, options: ["Parquet", "Laminate", "Tile", "Other"] },
    { name: "atticMaterial", kind: "text", maxLength: 100 },
    { name: "roofMaterial", kind: "enum", required: true, options: ["Tile", "Metal", "Other"] },
    { name: "windowType", kind: "enum", required: true, options: ["Thermopane", "Wood", "Other"] },
  ],
  Land: [
    {
      name: "landDesignation",
      kind: "enum",
      required: true,
      options: ["Intravilan", "Extravilan", "Agricultural", "Construction"],
    },
    { name: "roadAccess", kind: "enum", options: ["Paved", "Gravel", "None"] },
    { name: "utilitiesAtBoundary", kind: "flags", flags: ["water", "electricity", "gas"] },
  ],
  Commercial: [
    { name: "spaceType", kind: "enum", required: true, options: ["Office", "Retail", "Warehouse", "HoReCa"] },
    { name: "floor", kind: "int", min: -5, max: 200 },
    { name: "mainStreetAccess", kind: "bool" },
    { name: "electricalPower", kind: "text", maxLength: 50 },
  ],
  Garage: [{ name: "garageType", kind: "enum", required: true, options: ["Underground", "Box", "Individual"] }],
  Room: [
    { name: "privateOrSharedBathroom", kind: "enum", required: true, options: ["Private", "Shared"] },
    { name: "roommateCount", kind: "int", min: 0, max: 20 },
  ],
};

// Year built and condition don't apply to a bare plot of land.
export function hasBuilding(propertyType: string): boolean {
  return propertyType !== "Land";
}

// A House uses its own, more granular houseCondition attribute instead of the general condition.
export function usesGeneralCondition(propertyType: string): boolean {
  return propertyType !== "Land" && propertyType !== "House";
}

// Reads a submitted/current form value by input name (FormData.get-shaped).
export type ValueGetter = (name: string) => string | null;

export function isFieldVisible(field: AttributeField, get: ValueGetter): boolean {
  if (!field.visibleWhen) return true;
  const controlling = get(attributeInputName(field.visibleWhen.field));
  return controlling !== null && field.visibleWhen.values.includes(controlling);
}

export function isFieldRequired(field: AttributeField): boolean {
  return "required" in field && field.required === true;
}

export function attributeSchemaFor(propertyType: string): readonly AttributeField[] {
  return ATTRIBUTE_SCHEMA[propertyType as PropertyTypeName] ?? [];
}

// Form inputs for attributes are named "attr.<field>" (and "attr.<field>.<flag>" for flags).
export function attributeInputName(field: string, flag?: string): string {
  return flag ? `attr.${field}.${flag}` : `attr.${field}`;
}

// Builds the typeSpecificAttributes object for the selected type only — fields that belong to
// another type are never sent (the backend would reject them).
export function readAttributes(propertyType: string, formData: FormData): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  const get: ValueGetter = (name) => {
    const value = formData.get(name);
    return typeof value === "string" ? value : null;
  };

  for (const field of attributeSchemaFor(propertyType)) {
    // A conditional field whose condition isn't met is never sent — the backend rejects it.
    if (!isFieldVisible(field, get)) continue;
    const raw = formData.get(attributeInputName(field.name));
    switch (field.kind) {
      case "int":
      case "decimal":
        if (raw !== null && raw !== "" && !Number.isNaN(Number(raw))) result[field.name] = Number(raw);
        break;
      case "enum":
      case "text":
        if (typeof raw === "string" && raw.trim() !== "") result[field.name] = raw.trim();
        break;
      case "bool":
        result[field.name] = raw === "true";
        break;
      case "flags": {
        const values = Object.fromEntries(
          field.flags.map((flag) => [flag, formData.get(attributeInputName(field.name, flag)) === "true"]),
        );
        if (Object.values(values).some(Boolean)) result[field.name] = values;
        break;
      }
    }
  }

  return result;
}
