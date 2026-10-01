import {
  amenitiesForSection,
  detailLayoutFor,
  isAmenitySection,
  isProximitySection,
  sectionsFor,
  type CoreField,
  type DetailSectionId,
} from "@/lib/property/detailLayouts";
import { attributeSchemaFor, isFieldVisible, type AttributeField } from "@/lib/property/attributeSchema";
import { petsApplyTo } from "@/lib/property/rentalFields";
import type { Amenity, Listing, Proximity, RentalDetails } from "@/types/listing";

// The listing page's "Details": exactly what the owner filled in, grouped by the section of the
// form they filled it in (DETAIL_LAYOUTS — same titles and icons), in the form's order. Nothing
// is shown that wasn't answered; a section with nothing in it doesn't appear. Pure data — the page
// turns it into text (labels and formatting need the translations).

export type DetailFact =
  | { kind: "core"; field: CoreField; value: number | string }
  // totalFloors (or totalFloorsInBuilding) is folded into the floor fact: "3 of 9".
  | { kind: "attribute"; field: AttributeField; value: unknown; ofFloors?: number }
  | { kind: "rental"; field: RentalFact; value: unknown };

export type RentalFact = "petsAllowed" | "minLeasePeriodMonths" | "securityDepositAmount" | "availableFrom" | "utilitiesIncluded";

export type DetailSectionView = {
  id: DetailSectionId;
  facts: DetailFact[];
  amenities: Amenity[];
  proximities: Proximity[];
};

// A building's floor count: totalFloors (Apartment) or totalFloorsInBuilding (Commercial).
const FLOOR_TOTALS = ["totalFloors", "totalFloorsInBuilding"];

function floorsTotal(attributes: Record<string, unknown>): number | undefined {
  const total = FLOOR_TOTALS.map((name) => attributes[name]).find((v) => typeof v === "number");
  return total as number | undefined;
}

function isAnswered(value: unknown): boolean {
  return value !== null && value !== undefined && !(typeof value === "string" && value.trim() === "");
}

export function listingDetailSections(listing: Pick<Listing, "transactionType" | "property" | "rentalDetails">): DetailSectionView[] {
  const { property, transactionType, rentalDetails } = listing;
  const layout = detailLayoutFor(property.propertyType);
  if (!layout) return [];

  const attributes = property.typeSpecificAttributes ?? {};
  const schema = attributeSchemaFor(property.propertyType);
  // A conditional field (heating energy source, office count, ...) only counts while its
  // condition holds — the same rule the form applies (it reads the "attr.<field>" inputs).
  const get = (name: string) => {
    const field = name.startsWith("attr.") ? name.slice(5) : name;
    const value = attributes[field];
    return isAnswered(value) ? String(value) : null;
  };

  const views: DetailSectionView[] = [];
  for (const section of sectionsFor(layout, transactionType)) {
    const facts: DetailFact[] = [];

    for (const core of section.coreFields) {
      const value = property[core];
      if (isAnswered(value)) facts.push({ kind: "core", field: core, value: value as number | string });
    }

    for (const name of section.attributeFields) {
      const field = schema.find((f) => f.name === name);
      const value = attributes[name];
      if (!field || !isAnswered(value) || !isFieldVisible(field, get)) continue;
      // Shown with the floor instead ("3 of 9").
      if (FLOOR_TOTALS.includes(name) && isAnswered(attributes.floor) && section.attributeFields.includes("floor")) continue;

      const total = name === "floor" ? floorsTotal(attributes) : undefined;
      facts.push(total !== undefined ? { kind: "attribute", field, value, ofFloors: total } : { kind: "attribute", field, value });
    }

    for (const rental of section.rentalFields ?? []) {
      if (rental === "petsAllowed" && !petsApplyTo(property.propertyType)) continue;
      const value = rentalDetails?.[rental];
      if (isAnswered(value)) facts.push({ kind: "rental", field: rental, value });
    }

    const selectedIds = new Set(property.amenities.map((a) => a.id));
    const amenities = isAmenitySection(section)
      ? amenitiesForSection(layout, section, property.amenities, property.propertyType).filter((a) => selectedIds.has(a.id))
      : [];
    const proximities = isProximitySection(section) ? property.proximities : [];

    if (facts.length > 0 || amenities.length > 0 || proximities.length > 0) {
      views.push({ id: section.id, facts, amenities, proximities });
    }
  }

  return views;
}

// A section that's only a set of picks (amenities, nearby places) — the page shows these first,
// as highlights; the others hold label/value facts.
export function isHighlightSection(section: DetailSectionView): boolean {
  return section.facts.length === 0 && (section.amenities.length > 0 || section.proximities.length > 0);
}

// The lease terms from the form's "Price & terms" step — shown with the price, where they were
// entered (pets are a house rule, in the Details step's "Rental rules" section instead).
export function rentalTermFacts(transactionType: string, rental: RentalDetails | null): DetailFact[] {
  if (transactionType !== "Rent" || !rental) return [];
  const order: RentalFact[] = ["minLeasePeriodMonths", "securityDepositAmount", "availableFrom", "utilitiesIncluded"];
  return order.filter((f) => isAnswered(rental[f])).map((f) => ({ kind: "rental", field: f, value: rental[f] }));
}

// The headline numbers under the title (like "3 camere · 72 m² · etaj 4 din 9"), per property type,
// most telling first — only the ones answered, at most four.
export type KeyFactId =
  | "rooms"
  | "area"
  | "landArea"
  | "floor"
  | "houseFloors"
  | "bathrooms"
  | "yearBuilt"
  | "plotType"
  | "spaceType"
  | "parkingType"
  | "bathroomType"
  | "roommateCount";

export type KeyFact = { id: KeyFactId; value: number | string; ofFloors?: number };

const KEY_FACTS: Record<string, readonly KeyFactId[]> = {
  Apartment: ["rooms", "area", "floor", "yearBuilt"],
  House: ["rooms", "area", "landArea", "houseFloors"],
  Land: ["area", "plotType"],
  Commercial: ["area", "spaceType", "floor", "yearBuilt"],
  Garage: ["area", "parkingType", "yearBuilt"],
  Room: ["area", "bathroomType", "roommateCount"],
};

const KEY_FACT_SOURCE: Partial<Record<KeyFactId, string>> = {
  rooms: "rooms",
  landArea: "landAreaM2",
  houseFloors: "houseFloors",
  bathrooms: "bathrooms",
  plotType: "plotType",
  spaceType: "spaceType",
  parkingType: "parkingType",
  bathroomType: "bathroomType",
  roommateCount: "roommateCount",
};

export function keyFacts(property: Listing["property"]): KeyFact[] {
  const attributes = property.typeSpecificAttributes ?? {};
  const facts: KeyFact[] = [];
  for (const id of KEY_FACTS[property.propertyType] ?? []) {
    let fact: KeyFact | null = null;
    if (id === "area") fact = { id, value: property.totalAreaM2 };
    else if (id === "yearBuilt") fact = property.yearBuilt != null ? { id, value: property.yearBuilt } : null;
    else if (id === "floor") {
      const total = floorsTotal(attributes);
      fact = typeof attributes.floor === "number" ? { id, value: attributes.floor, ...(total !== undefined ? { ofFloors: total } : {}) } : null;
    } else {
      const value = attributes[KEY_FACT_SOURCE[id]!];
      fact = isAnswered(value) ? { id, value: value as number | string } : null;
    }
    // A Room migrated without an area has 0 — not worth a headline.
    if (fact && !(id === "area" && !(property.totalAreaM2 > 0))) facts.push(fact);
  }
  return facts.slice(0, 4);
}

