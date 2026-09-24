// Mirrors the backend's per-PropertyType TypeSpecificAttributes schemas
// (Imova.Domain.Properties.Attributes) and which of their fields are required
// (Imova.Application.Features.Listings.Attributes.PropertyAttributesValidator) — the backend is
// the source of truth and validates independently; this only drives the form. Keep them in sync.

export const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;
export type PropertyTypeName = (typeof PROPERTY_TYPES)[number];

export type AttributeField =
  | { name: string; kind: "int"; required?: boolean; min: number; max: number }
  | { name: string; kind: "decimal"; required?: boolean; min: number }
  | { name: string; kind: "enum"; required?: boolean; options: readonly string[] }
  | { name: string; kind: "bool" }
  | { name: string; kind: "text"; maxLength: number }
  // A nested object of booleans, e.g. utilities: { water, sewage, gas, electricity }.
  | { name: string; kind: "flags"; flags: readonly string[] };

export const ATTRIBUTE_SCHEMA: Record<PropertyTypeName, readonly AttributeField[]> = {
  Apartment: [
    { name: "rooms", kind: "int", required: true, min: 1, max: 50 },
    { name: "floor", kind: "int", required: true, min: -5, max: 200 },
    { name: "totalFloors", kind: "int", required: true, min: 1, max: 200 },
    { name: "bathrooms", kind: "int", min: 0, max: 20 },
    { name: "heatingType", kind: "enum", options: ["Centralized", "Autonomous", "Other"] },
  ],
  House: [
    { name: "rooms", kind: "int", required: true, min: 1, max: 100 },
    { name: "houseFloors", kind: "int", required: true, min: 1, max: 10 },
    { name: "landAreaM2", kind: "decimal", min: 0.01 },
    { name: "constructionType", kind: "enum", options: ["Brick", "Stone", "Wood", "Other"] },
    { name: "utilities", kind: "flags", flags: ["water", "sewage", "gas", "electricity"] },
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

  for (const field of attributeSchemaFor(propertyType)) {
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
