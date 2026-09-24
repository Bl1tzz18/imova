// Mirrors the backend's per-PropertyType TypeSpecificAttributes schemas
// (Imova.Domain.Properties.Attributes) and which of their fields are required
// (Imova.Application.Features.Listings.Attributes.PropertyAttributesValidator) — the backend is
// the source of truth and validates independently; this only drives the form. Keep them in sync.

export const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;
export type PropertyTypeName = (typeof PROPERTY_TYPES)[number];

// A field that only exists while another field has one of the given values (e.g. heating energy
// source, only for boiler/heat-pump/solar heating). While hidden it's never submitted, and
// `required` applies only while it's visible.
export type Visibility = { field: string; values: readonly string[] };

type FieldBase = { name: string; visibleWhen?: Visibility };

export type AttributeField = FieldBase &
  (
    | { kind: "int"; required?: boolean; min: number; max: number }
    | { kind: "decimal"; required?: boolean; min: number; max?: number }
    | { kind: "enum"; required?: boolean; options: readonly string[] }
    // A true/false answer asked as a Yes/No dropdown (so "not answered" is distinct from "no").
    | { kind: "yesno"; required?: boolean }
    | { kind: "text"; maxLength: number }
  );

// --- Vocabularies shared across property types (mirror the shared backend enums) ---

const BUILDING_MATERIALS = [
  "Brick", "Panel", "ConcreteBlock", "LimestoneBlock", "Monolith", "Concrete", "Combined", "AeratedConcrete",
  "Wood", "Adobe", "Other",
] as const;

// Most common first, as listings in Moldova typically describe themselves (display order only —
// no usage stats exist yet to sort by).
const FINISH_CONDITIONS = [
  "EuroRenovated", "CosmeticRepair", "WhiteStructure", "GrayStructure", "IndividualDesign", "NeedsRepair",
  "NoRepair", "Unfinished", "ToBeDemolished",
] as const;

const HEATING_SYSTEMS = [
  "OwnBoiler", "Convector", "InfraredPanels", "DistrictHeating", "HeatPump", "SolarPanels", "Stove", "None",
] as const;

const FLOOR_MATERIALS = ["Parquet", "Laminate", "Tile", "Other"] as const;

// Mirrors Heating.RequiresDetails on the backend.
export const HEATING_WITH_OWN_SOURCE = ["OwnBoiler", "HeatPump", "SolarPanels"] as const;

// Heating system + its conditional energy source/distribution — identical for House and Apartment.
const HEATING_FIELDS: readonly AttributeField[] = [
  { name: "heatingSystem", kind: "enum", required: true, options: HEATING_SYSTEMS },
  {
    name: "heatingEnergySource",
    kind: "enum",
    required: true,
    options: ["Gas", "Electricity", "Wood", "Combined"],
    visibleWhen: { field: "heatingSystem", values: HEATING_WITH_OWN_SOURCE },
  },
  {
    name: "heatingDistribution",
    kind: "enum",
    required: true,
    options: ["Radiators", "UnderfloorHeating", "Air"],
    visibleWhen: { field: "heatingSystem", values: HEATING_WITH_OWN_SOURCE },
  },
];

export const ATTRIBUTE_SCHEMA: Record<PropertyTypeName, readonly AttributeField[]> = {
  Apartment: [
    // Type & structure
    { name: "housingStockType", kind: "enum", required: true, options: ["Existing", "NewConstruction"] },
    { name: "buildingMaterial", kind: "enum", required: true, options: BUILDING_MATERIALS },
    { name: "finishCondition", kind: "enum", required: true, options: FINISH_CONDITIONS },
    { name: "layout", kind: "enum", required: true, options: ["Studio", "IndividualLayout", "SovietEra", "Dormitory", "Other"] },
    { name: "rooms", kind: "int", required: true, min: 1, max: 50 },
    { name: "floor", kind: "int", required: true, min: -5, max: 200 },
    { name: "totalFloors", kind: "int", required: true, min: 1, max: 200 },
    { name: "bathrooms", kind: "int", min: 0, max: 20 },
    // Areas
    { name: "livingAreaM2", kind: "decimal", min: 0.01 },
    { name: "kitchenAreaM2", kind: "decimal", min: 0.01 },
    // Systems & utilities
    ...HEATING_FIELDS,
    { name: "gasSupply", kind: "yesno", required: true },
    // Finishing materials
    { name: "floorMaterial", kind: "enum", required: true, options: FLOOR_MATERIALS },
  ],
  House: [
    // Type & structure
    { name: "houseType", kind: "enum", required: true, options: ["Individual", "Duplex", "Triplex", "Townhouse", "Villa", "Other"] },
    { name: "buildingMaterial", kind: "enum", required: true, options: BUILDING_MATERIALS },
    { name: "finishCondition", kind: "enum", required: true, options: FINISH_CONDITIONS },
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
    ...HEATING_FIELDS,
    { name: "waterSupply", kind: "enum", required: true, options: ["CentralNetwork", "Well", "DrilledWell", "Cistern", "None"] },
    { name: "sewerage", kind: "enum", required: true, options: ["Central", "SepticTank", "None"] },
    { name: "gasSupply", kind: "yesno", required: true },
    // Finishing materials
    { name: "floorMaterial", kind: "enum", required: true, options: FLOOR_MATERIALS },
    { name: "atticMaterial", kind: "enum", options: ["Wood", "Drywall", "Osb", "Brick", "AeratedConcrete", "Other"] },
    { name: "roofMaterial", kind: "enum", required: true, options: ["Tile", "Metal", "Other"] },
    { name: "windowType", kind: "enum", required: true, options: ["Thermopane", "Wood", "Other"] },
  ],
  Land: [
    // Type & area (the plot's area is the general totalAreaM2)
    {
      name: "plotType",
      kind: "enum",
      required: true,
      options: ["Agricultural", "WithPlantations", "Forest", "Garden", "Industrial", "NearLake", "ForConstruction"],
    },
    { name: "locationContext", kind: "enum", required: true, options: ["WithinTownLimits", "OutsideTownLimits"] },
    // The "bonitate" soil-quality index — agricultural plots only.
    {
      name: "soilQualityScore",
      kind: "int",
      min: 1,
      max: 100,
      visibleWhen: { field: "plotType", values: ["Agricultural"] },
    },
    // Utilities & access
    { name: "roadAccess", kind: "enum", required: true, options: ["Paved", "Gravel", "None"] },
    { name: "gasPipelineAtBoundary", kind: "yesno", required: true },
    { name: "electricitySupplyAtBoundary", kind: "yesno", required: true },
    { name: "sewerageAtBoundary", kind: "yesno", required: true },
    { name: "irrigationSystem", kind: "yesno", required: true },
    { name: "phoneLineAvailable", kind: "yesno", required: true },
  ],
  Commercial: [
    // Type & structure
    {
      name: "spaceType",
      kind: "enum",
      required: true,
      options: [
        "OfficeSpace", "RetailSpace", "Warehouse", "FoodServiceSpace", "IndustrialSpace", "UniversalSpace",
        "BeautySalon", "AutoService", "DentalSpace", "SportsSpace", "ConferenceRoom", "ResortOrHotel",
      ],
    },
    { name: "finishCondition", kind: "enum", required: true, options: FINISH_CONDITIONS },
    // Negative for basement levels: -1 = basement, 0 = semi-basement.
    { name: "floor", kind: "int", required: true, min: -5, max: 200 },
    { name: "totalFloorsInBuilding", kind: "int", min: 1, max: 200 },
    // Areas
    { name: "workingAreaM2", kind: "decimal", min: 0.01 },
    {
      name: "numberOfOffices",
      kind: "int",
      min: 1,
      max: 500,
      visibleWhen: { field: "spaceType", values: ["OfficeSpace"] },
    },
    // Systems & utilities
    { name: "bathrooms", kind: "int", required: true, min: 0, max: 50 },
    { name: "phoneLinesCount", kind: "int", min: 0, max: 100 },
    { name: "mainStreetAccess", kind: "yesno", required: true },
    { name: "electricalPower", kind: "text", maxLength: 50 },
    { name: "gasSupply", kind: "yesno", required: true },
  ],
  Garage: [{ name: "parkingType", kind: "enum", required: true, options: ["Garage", "ParkingSpot", "UndergroundParking"] }],
  Room: [
    { name: "bathroomType", kind: "enum", required: true, options: ["Private", "Shared"] },
    { name: "roommateCount", kind: "int", min: 0, max: 20 },
  ],
};

// Year built doesn't apply to a bare plot of land.
export function hasBuilding(propertyType: string): boolean {
  return propertyType !== "Land";
}

// House, Apartment and Commercial describe their state with the finishCondition attribute; only
// Garage and Room still use the general condition (Land has none).
export function usesGeneralCondition(propertyType: string): boolean {
  return propertyType === "Garage" || propertyType === "Room";
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

// The fields other fields' visibility depends on (e.g. heatingSystem, plotType) — the form
// tracks their live values to show/hide the dependent fields.
export function controllingFields(propertyType: string): string[] {
  return [
    ...new Set(
      attributeSchemaFor(propertyType)
        .map((f) => f.visibleWhen?.field)
        .filter((name): name is string => name !== undefined),
    ),
  ];
}

// Form inputs for attributes are named "attr.<field>".
export function attributeInputName(field: string): string {
  return `attr.${field}`;
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
      case "yesno":
        if (raw === "true" || raw === "false") result[field.name] = raw === "true";
        break;
    }
  }

  return result;
}
