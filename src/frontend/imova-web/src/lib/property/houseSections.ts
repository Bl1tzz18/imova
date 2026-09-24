import type { Amenity } from "@/types/listing";
import {
  ATTRIBUTE_SCHEMA,
  isFieldRequired,
  isFieldVisible,
  attributeInputName,
  type ValueGetter,
} from "@/lib/property/attributeSchema";

// The seven collapsible sections of the "Details" step for a House, in display order. Four hold
// fields (general Property fields and House TypeSpecificAttributes); the last three hold the
// amenities of one category each.
export type HouseSectionId = "structure" | "areas" | "systems" | "finishing" | "comfort" | "security" | "leisure";

export type AmenityCategory = "General" | "Comfort" | "Security" | "Leisure";

export type HouseSection = {
  id: HouseSectionId;
  // General Property inputs (not attributes) placed in this section.
  coreFields: readonly ("totalAreaM2" | "yearBuilt")[];
  // House TypeSpecificAttributes placed in this section, in display order.
  attributeFields: readonly string[];
  amenityCategory?: AmenityCategory;
};

export const HOUSE_SECTIONS: readonly HouseSection[] = [
  {
    id: "structure",
    coreFields: ["yearBuilt"],
    attributeFields: ["houseType", "buildingMaterial", "houseCondition", "houseFloors", "rooms", "ceilingHeightM"],
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
  { id: "comfort", coreFields: [], attributeFields: [], amenityCategory: "Comfort" },
  { id: "security", coreFields: [], attributeFields: [], amenityCategory: "Security" },
  { id: "leisure", coreFields: [], attributeFields: [], amenityCategory: "Leisure" },
];

// Core fields that must be filled in (yearBuilt is optional).
const REQUIRED_CORE_FIELDS = new Set(["totalAreaM2"]);

function isFilled(value: string | null): boolean {
  return value !== null && value.trim() !== "";
}

function houseField(name: string) {
  const field = ATTRIBUTE_SCHEMA.House.find((f) => f.name === name);
  if (!field) throw new Error(`Unknown House attribute "${name}"`);
  return field;
}

export function hasRequiredFields(section: HouseSection): boolean {
  return (
    section.coreFields.some((name) => REQUIRED_CORE_FIELDS.has(name)) ||
    section.attributeFields.some((name) => isFieldRequired(houseField(name)))
  );
}

// A section with required fields is complete once each of them that's currently shown (the
// heating details only exist for some heating systems) has a value. The amenity sections have
// nothing required, so for them "complete" means the user has opened and looked at them.
export function isSectionComplete(section: HouseSection, get: ValueGetter, visited: boolean): boolean {
  if (!hasRequiredFields(section)) return visited;

  const coreComplete = section.coreFields
    .filter((name) => REQUIRED_CORE_FIELDS.has(name))
    .every((name) => isFilled(get(name)));

  const attributesComplete = section.attributeFields
    .map(houseField)
    .filter((field) => isFieldRequired(field) && isFieldVisible(field, get))
    .every((field) => isFilled(get(attributeInputName(field.name))));

  return coreComplete && attributesComplete;
}

// The "furnished" amenity isn't offered for a rental — RentalDetails.FurnishedStatus already
// captures it (mirrors Amenity.IsSelectableFor on the backend, which also rejects it).
export function selectableAmenities(amenities: readonly Amenity[], transactionType: string): Amenity[] {
  return amenities.filter((a) => !(transactionType === "Rent" && a.key === "furnished"));
}

export function amenitiesInCategory(
  amenities: readonly Amenity[],
  category: AmenityCategory,
  transactionType: string,
): Amenity[] {
  return selectableAmenities(amenities, transactionType).filter((a) => a.category === category);
}
