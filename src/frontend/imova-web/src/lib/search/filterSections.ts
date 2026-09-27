import type { Amenity, Proximity } from "@/types/listing";
import {
  DETAIL_LAYOUTS,
  amenitiesForSection,
  isAmenitySection,
  isProximitySection,
  type DetailSectionId,
} from "@/lib/property/detailLayouts";
import {
  RENTAL_PARAMS,
  applicableTypeFilters,
  rentalFiltersApply,
  singlePropertyType,
  type SearchState,
  type TypeSpecificFilter,
} from "./filters";

// How the /search filter panel is split into collapsible sections. With exactly one property type
// picked, its filters are grouped like the listing form's "Details" step (DETAIL_LAYOUTS) — rooms
// under "Tip și structură", heating under "Sisteme și utilități", amenities by category — so
// searching reads like the form the listing was created with. Otherwise there's one generic
// section each for area and amenities.

export type FilterSectionId = "basics" | "location" | "price" | "area" | "rentalTerms" | DetailSectionId;

export type FilterSection = {
  id: FilterSectionId;
  // Whether the section holds the general area range (a type's "areas"/"typeArea" section does).
  area: boolean;
  // Type-specific filters shown here, in the listing form's order.
  typeFilters: readonly TypeSpecificFilter[];
  // Amenity checkboxes shown here (amenity sections only).
  amenities: readonly Amenity[];
  // How many filters in this section are set — shown on its header.
  activeCount: number;
  // Starts open (the panel also opens any section with an active filter).
  openByDefault: boolean;
};

const has = (state: SearchState, param: string) => (state[param]?.length ?? 0) > 0;

function section(id: FilterSectionId, activeCount: number, extra: Partial<FilterSection> = {}): FilterSection {
  return { id, area: false, typeFilters: [], amenities: [], activeCount, openByDefault: false, ...extra };
}

function areaActive(state: SearchState): boolean {
  return has(state, "minAreaM2") || has(state, "maxAreaM2");
}

function amenityCount(state: SearchState, amenities: readonly Amenity[]): number {
  const selected = new Set(state.amenityIds ?? []);
  return amenities.filter((a) => selected.has(a.id)).length;
}

export function filterSections(state: SearchState, amenities: readonly Amenity[], proximities: readonly Proximity[]): FilterSection[] {
  const type = singlePropertyType(state);
  const selectedTypes = state.propertyType ?? [];

  const sections: FilterSection[] = [
    section("basics", (has(state, "transactionType") ? 1 : 0) + selectedTypes.length, { openByDefault: true }),
    section("location", has(state, "raionId") ? 1 : 0, { openByDefault: true }),
    section("price", has(state, "minPriceEur") || has(state, "maxPriceEur") ? 1 : 0, { openByDefault: true }),
  ];

  if (type) {
    const layout = DETAIL_LAYOUTS[type];
    const applicable = applicableTypeFilters(state);
    let first = true;
    for (const detail of layout.sections) {
      if (isAmenitySection(detail) || isProximitySection(detail) || detail.rentalFields) continue;
      const withArea = detail.coreFields.includes("totalAreaM2");
      const typeFilters = detail.attributeFields.flatMap((field) => applicable.filter((f) => f.field === field));
      if (!withArea && typeFilters.length === 0) continue;
      const activeCount =
        (withArea && areaActive(state) ? 1 : 0) + typeFilters.filter((f) => f.params.some((p) => has(state, p))).length;
      sections.push(section(detail.id, activeCount, { area: withArea, typeFilters, openByDefault: first }));
      first = false;
    }
  } else {
    sections.push(section("area", areaActive(state) ? 1 : 0, { area: true }));
  }

  if (rentalFiltersApply(state)) {
    sections.push(section("rentalTerms", RENTAL_PARAMS.filter((p) => has(state, p)).length));
  }

  if (type) {
    const layout = DETAIL_LAYOUTS[type];
    for (const detail of layout.sections.filter(isAmenitySection)) {
      const items = amenitiesForSection(layout, detail, amenities, type);
      if (items.length > 0) sections.push(section(detail.id, amenityCount(state, items), { amenities: items }));
    }
  } else {
    // Amenities that apply to at least one selected type (all of them while no type is picked).
    const offered = amenities.filter(
      (a) => selectedTypes.length === 0 || selectedTypes.some((t) => a.applicablePropertyTypes.includes(t)),
    );
    if (offered.length > 0) sections.push(section("amenities", amenityCount(state, offered), { amenities: offered }));
  }

  if (proximities.length > 0) {
    const selected = new Set(state.proximityIds ?? []);
    sections.push(section("proximities", proximities.filter((p) => selected.has(p.id)).length));
  }

  return sections;
}
