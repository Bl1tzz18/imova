import type { Amenity, Proximity } from "@/types/listing";
import {
  attributeInputName,
  attributeSchemaFor,
  isFieldRequired,
  isFieldVisible,
  type PropertyTypeName,
  type ValueGetter,
} from "@/lib/property/attributeSchema";
import { rentalInputName, type RentalField } from "@/lib/property/rentalFields";

// How the listing form's "Details" step is split into collapsible sections, per property type.

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
  | "amenities"
  | "proximities"
  | "rentalRules";

// condition is the general Property.Condition — only Garage and Room use it (see usesGeneralCondition).
export type CoreField = "totalAreaM2" | "yearBuilt" | "condition";

export type DetailSection = {
  id: DetailSectionId;
  // General Property inputs (not attributes) placed in this section.
  coreFields: readonly CoreField[];
  // TypeSpecificAttributes placed in this section, in display order.
  attributeFields: readonly string[];
  // Amenity sections list the categories they show; the catch-all one (at most one per layout —
  // none for Land, which has no amenities) also takes every applicable amenity whose category no
  // other section of the layout shows.
  amenityCategories?: readonly AmenityCategory[];
  catchAllAmenities?: boolean;
  // The "Vecinătăți" section: what the property is close to (see Proximity), kept apart from the
  // amenities, which describe the property itself.
  proximities?: boolean;
  // RentalDetails fields asked here; a section with these only exists for a rental.
  rentalFields?: readonly RentalField[];
};

export type DetailLayout = {
  // Which PropertyForm label the total-area input uses ("total area", the plot's area, or plain
  // "area" for a garage/room).
  totalAreaLabel: "houseTotalAreaLabel" | "landAreaLabel" | "areaLabel";
  sections: readonly DetailSection[];
};

const noFields = { coreFields: [], attributeFields: [] } as const;

// What the property is near (school, park, ...) — every layout lists it right after its amenities.
const PROXIMITIES: DetailSection = { id: "proximities", ...noFields, proximities: true };

// Rental-only: the house rules of a rented home (pets).
const RENTAL_RULES: DetailSection = { id: "rentalRules", ...noFields, rentalFields: ["petsAllowed"] };

export const DETAIL_LAYOUTS: Record<PropertyTypeName, DetailLayout> = {
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
      PROXIMITIES,
      RENTAL_RULES,
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
      PROXIMITIES,
      RENTAL_RULES,
    ],
  },
  Land: {
    totalAreaLabel: "landAreaLabel",
    // No amenity section: no amenity applies to Land (its access/utilities are attributes).
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
      PROXIMITIES,
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
      PROXIMITIES,
    ],
  },
  Garage: {
    totalAreaLabel: "areaLabel",
    sections: [
      { id: "typeArea", coreFields: ["totalAreaM2", "yearBuilt", "condition"], attributeFields: ["parkingType"] },
      { id: "amenities", ...noFields, catchAllAmenities: true },
      PROXIMITIES,
    ],
  },
  Room: {
    totalAreaLabel: "areaLabel",
    sections: [
      {
        id: "typeArea",
        coreFields: ["totalAreaM2", "yearBuilt", "condition"],
        attributeFields: ["bathroomType", "roommateCount"],
      },
      { id: "comfort", ...noFields, amenityCategories: ["Comfort"] },
      // Balcony, building heating type — and anything else that applies.
      { id: "other", ...noFields, amenityCategories: ["General"], catchAllAmenities: true },
      PROXIMITIES,
      RENTAL_RULES,
    ],
  },
};

export function detailLayoutFor(propertyType: string): DetailLayout | undefined {
  return DETAIL_LAYOUTS[propertyType as PropertyTypeName];
}

// The sections shown for a transaction type — rental-only ones disappear for a sale.
export function sectionsFor(layout: DetailLayout, transactionType: string): DetailSection[] {
  return layout.sections.filter((s) => !s.rentalFields || transactionType === "Rent");
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

export function isProximitySection(section: DetailSection): boolean {
  return section.proximities === true;
}

export function hasRequiredFields(section: DetailSection, propertyType: string): boolean {
  return (
    (section.rentalFields?.length ?? 0) > 0 ||
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

  // Every rental field asked in the details step (pets) is a required Yes/No.
  const rentalComplete = (section.rentalFields ?? []).every((field) => isFilled(get(rentalInputName(field))));

  return coreComplete && attributesComplete && rentalComplete;
}

// The amenities the form offers for a property type: those that apply to it (mirrors
// Amenity.ApplicablePropertyTypes on the backend, which rejects any other). "Furnished" is offered
// for sale and rent alike — it's the only furnishing question.
export function selectableAmenities(amenities: readonly Amenity[], propertyType: string): Amenity[] {
  return amenities.filter((a) => a.applicablePropertyTypes.includes(propertyType));
}

export function amenitiesForSection(
  layout: DetailLayout,
  section: DetailSection,
  amenities: readonly Amenity[],
  propertyType: string,
): Amenity[] {
  const shownElsewhere = new Set(layout.sections.flatMap((s) => (s === section ? [] : (s.amenityCategories ?? []))));
  const ownCategories = new Set(section.amenityCategories ?? []);

  return selectableAmenities(amenities, propertyType).filter(
    (a) =>
      ownCategories.has(a.category as AmenityCategory) ||
      (section.catchAllAmenities === true && !shownElsewhere.has(a.category as AmenityCategory)),
  );
}

// The proximities the form offers for a property type (mirrors Proximity.ApplicablePropertyTypes on
// the backend — currently every one applies to every type).
export function selectableProximities(proximities: readonly Proximity[], propertyType: string): Proximity[] {
  return proximities.filter((p) => p.applicablePropertyTypes.includes(propertyType));
}
