import type { Amenity } from "@/types/listing";
import {
  attributeInputName,
  attributeSchemaFor,
  isFieldRequired,
  isFieldVisible,
  type PropertyTypeName,
  type ValueGetter,
} from "@/lib/property/attributeSchema";

// How the listing form's "Details" step is split into collapsible sections, per property type.
// Types without a layout (Garage, Room — only a field or two) use the flat form instead.

export type AmenityCategory = "General" | "Comfort" | "Security" | "Leisure";

export type DetailSectionId =
  | "structure"
  | "areas"
  | "systems"
  | "finishing"
  | "comfort"
  | "security"
  | "leisure"
  | "other"
  | "typeArea"
  | "utilitiesAccess"
  | "surroundings"
  | "amenities";

export type CoreField = "totalAreaM2" | "yearBuilt";

export type DetailSection = {
  id: DetailSectionId;
  // General Property inputs (not attributes) placed in this section.
  coreFields: readonly CoreField[];
  // TypeSpecificAttributes placed in this section, in display order.
  attributeFields: readonly string[];
  // Amenity sections list the categories they show; the catch-all one also takes every applicable
  // amenity whose category no other section of the layout shows.
  amenityCategories?: readonly AmenityCategory[];
  catchAllAmenities?: boolean;
};

export type DetailLayout = {
  // Which PropertyForm label the total-area input uses ("total area" vs. the plot's area).
  totalAreaLabel: "houseTotalAreaLabel" | "landAreaLabel";
  sections: readonly DetailSection[];
};

const noFields = { coreFields: [], attributeFields: [] } as const;

export const DETAIL_LAYOUTS: Partial<Record<PropertyTypeName, DetailLayout>> = {
  House: {
    totalAreaLabel: "houseTotalAreaLabel",
    sections: [
      {
        id: "structure",
        coreFields: ["yearBuilt"],
        attributeFields: ["houseType", "buildingMaterial", "finishCondition", "houseFloors", "rooms", "ceilingHeightM"],
      },
      {
        id: "areas",
        coreFields: ["totalAreaM2"],
        attributeFields: ["livingAreaM2", "landAreaM2", "kitchenAreaM2", "atticAreaM2", "basementAreaM2"],
      },
      {
        id: "systems",
        coreFields: [],
        attributeFields: ["heatingSystem", "heatingEnergySource", "heatingDistribution", "waterSupply", "sewerage", "gasSupply"],
      },
      { id: "finishing", coreFields: [], attributeFields: ["floorMaterial", "atticMaterial", "roofMaterial", "windowType"] },
      { id: "comfort", ...noFields, amenityCategories: ["Comfort"] },
      { id: "security", ...noFields, amenityCategories: ["Security"] },
      { id: "leisure", ...noFields, amenityCategories: ["Leisure"], catchAllAmenities: true },
    ],
  },
  Apartment: {
    totalAreaLabel: "houseTotalAreaLabel",
    sections: [
      {
        id: "structure",
        coreFields: ["yearBuilt"],
        attributeFields: [
          "housingStockType", "buildingMaterial", "finishCondition", "layout", "rooms", "floor", "totalFloors", "bathrooms",
        ],
      },
      { id: "areas", coreFields: ["totalAreaM2"], attributeFields: ["livingAreaM2", "kitchenAreaM2"] },
      {
        id: "systems",
        coreFields: [],
        attributeFields: ["heatingSystem", "heatingEnergySource", "heatingDistribution", "gasSupply"],
      },
      { id: "finishing", coreFields: [], attributeFields: ["floorMaterial"] },
      { id: "comfort", ...noFields, amenityCategories: ["Comfort"] },
      { id: "security", ...noFields, amenityCategories: ["Security"] },
      // Elevator, balcony, annex, separate entrance — and anything else that applies.
      { id: "other", ...noFields, amenityCategories: ["General"], catchAllAmenities: true },
    ],
  },
  Land: {
    totalAreaLabel: "landAreaLabel",
    sections: [
      { id: "typeArea", coreFields: ["totalAreaM2"], attributeFields: ["plotType", "locationContext", "soilQualityScore"] },
      {
        id: "utilitiesAccess",
        coreFields: [],
        attributeFields: [
          "roadAccess", "gasPipelineAtBoundary", "electricitySupplyAtBoundary", "sewerageAtBoundary", "irrigationSystem",
          "phoneLineAvailable",
        ],
      },
      { id: "surroundings", ...noFields, catchAllAmenities: true },
    ],
  },
  Commercial: {
    totalAreaLabel: "houseTotalAreaLabel",
    sections: [
      {
        id: "structure",
        coreFields: ["yearBuilt"],
        attributeFields: ["spaceType", "finishCondition", "floor", "totalFloorsInBuilding"],
      },
      { id: "areas", coreFields: ["totalAreaM2"], attributeFields: ["workingAreaM2", "numberOfOffices"] },
      {
        id: "systems",
        coreFields: [],
        attributeFields: ["bathrooms", "phoneLinesCount", "mainStreetAccess", "electricalPower", "gasSupply"],
      },
      { id: "amenities", ...noFields, catchAllAmenities: true },
    ],
  },
};

export function detailLayoutFor(propertyType: string): DetailLayout | undefined {
  return DETAIL_LAYOUTS[propertyType as PropertyTypeName];
}

// Core fields that must be filled in (yearBuilt is optional).
const REQUIRED_CORE_FIELDS = new Set<CoreField>(["totalAreaM2"]);

function isFilled(value: string | null): boolean {
  return value !== null && value.trim() !== "";
}

function schemaField(propertyType: string, name: string) {
  const field = attributeSchemaFor(propertyType).find((f) => f.name === name);
  if (!field) throw new Error(`Unknown ${propertyType} attribute "${name}"`);
  return field;
}

export function isAmenitySection(section: DetailSection): boolean {
  return section.amenityCategories !== undefined || section.catchAllAmenities === true;
}

export function hasRequiredFields(section: DetailSection, propertyType: string): boolean {
  return (
    section.coreFields.some((name) => REQUIRED_CORE_FIELDS.has(name)) ||
    section.attributeFields.some((name) => isFieldRequired(schemaField(propertyType, name)))
  );
}

// A section with required fields is complete once each of them that's currently shown (some only
// exist for certain values of another field) has a value. Sections with nothing required — the
// amenity ones — count as complete once the user has opened and looked at them.
export function isSectionComplete(
  section: DetailSection,
  propertyType: string,
  get: ValueGetter,
  visited: boolean,
): boolean {
  if (!hasRequiredFields(section, propertyType)) return visited;

  const coreComplete = section.coreFields
    .filter((name) => REQUIRED_CORE_FIELDS.has(name))
    .every((name) => isFilled(get(name)));

  const attributesComplete = section.attributeFields
    .map((name) => schemaField(propertyType, name))
    .filter((field) => isFieldRequired(field) && isFieldVisible(field, get))
    .every((field) => isFilled(get(attributeInputName(field.name))));

  return coreComplete && attributesComplete;
}

// The amenities the form offers for a property type: those that apply to it (mirrors
// Amenity.ApplicablePropertyTypes on the backend) — minus "furnished" on a rental, which
// RentalDetails.FurnishedStatus already captures (the backend rejects both cases too).
export function selectableAmenities(
  amenities: readonly Amenity[],
  propertyType: string,
  transactionType: string,
): Amenity[] {
  return amenities.filter(
    (a) => a.applicablePropertyTypes.includes(propertyType) && !(transactionType === "Rent" && a.key === "furnished"),
  );
}

export function amenitiesForSection(
  layout: DetailLayout,
  section: DetailSection,
  amenities: readonly Amenity[],
  propertyType: string,
  transactionType: string,
): Amenity[] {
  const shownElsewhere = new Set(layout.sections.flatMap((s) => (s === section ? [] : (s.amenityCategories ?? []))));
  const ownCategories = new Set(section.amenityCategories ?? []);

  return selectableAmenities(amenities, propertyType, transactionType).filter(
    (a) =>
      ownCategories.has(a.category as AmenityCategory) ||
      (section.catchAllAmenities === true && !shownElsewhere.has(a.category as AmenityCategory)),
  );
}
