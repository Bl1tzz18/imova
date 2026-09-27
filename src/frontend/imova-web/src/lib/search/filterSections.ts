import type { Amenity, Proximity } from "@/types/listing";
import {
  DETAIL_LAYOUTS,
  amenitiesForSection,
  isAmenitySection,
  isProximitySection,
  type DetailSectionId,
} from "@/lib/property/detailLayouts";
import {
  CONDITION_PARAM,
  RENTAL_PARAMS,
  YEAR_BUILT_PARAMS,
  attributeFilterVisible,
  attributeFiltersFor,
  conditionApplies,
  rentalFiltersApply,
  singlePropertyType,
  yearBuiltApplies,
  type AttributeFilter,
  type SearchState,
} from "./filters";

// How the /search filter panel is split into collapsible sections. Once a property type is picked,
// the rest of the panel is that type's listing form "Details" step (DETAIL_LAYOUTS) turned into
// filters: the same sections in the same order, each field the form asks there offered as a
// filter (numbers as ranges, choices and yes/no as "any or this"), amenities by category. With no
// type picked, only what every listing has: area, amenities, proximities.

export type FilterSectionId = "basics" | "location" | "price" | "area" | "rentalTerms" | DetailSectionId;

// One filter row inside a section: the general area / year built / condition, or an attribute.
export type FilterItem =
  | { kind: "area" }
  | { kind: "yearBuilt" }
  | { kind: "condition" }
  | { kind: "attribute"; filter: AttributeFilter };

export type FilterSection = {
  id: FilterSectionId;
  items: readonly FilterItem[];
  // Amenity checkboxes shown here (amenity sections only).
  amenities: readonly Amenity[];
  // How many filters in this section are set — shown on its header.
  activeCount: number;
  // Starts open (the panel also opens any section with an active filter).
  openByDefault: boolean;
};

const has = (state: SearchState, param: string) => (state[param]?.length ?? 0) > 0;

function itemParams(item: FilterItem): readonly string[] {
  switch (item.kind) {
    case "area":
      return ["minAreaM2", "maxAreaM2"];
    case "yearBuilt":
      return YEAR_BUILT_PARAMS;
    case "condition":
      return [CONDITION_PARAM];
    case "attribute":
      return item.filter.params;
  }
}

function section(id: FilterSectionId, activeCount: number, extra: Partial<FilterSection> = {}): FilterSection {
  return { id, items: [], amenities: [], activeCount, openByDefault: false, ...extra };
}

function itemsSection(state: SearchState, id: FilterSectionId, items: FilterItem[]): FilterSection {
  const activeCount = items.filter((item) => itemParams(item).some((p) => has(state, p))).length;
  return section(id, activeCount, { items });
}

function amenitySection(state: SearchState, id: FilterSectionId, amenities: readonly Amenity[]): FilterSection {
  const selected = new Set(state.amenityIds ?? []);
  return section(id, amenities.filter((a) => selected.has(a.id)).length, { amenities });
}

export function filterSections(state: SearchState, amenities: readonly Amenity[], proximities: readonly Proximity[]): FilterSection[] {
  const type = singlePropertyType(state);
  const sections: FilterSection[] = [
    section("basics", (has(state, "transactionType") ? 1 : 0) + (type ? 1 : 0), { openByDefault: true }),
    section("location", has(state, "raionId") ? 1 : 0, { openByDefault: true }),
    section("price", has(state, "minPriceEur") || has(state, "maxPriceEur") ? 1 : 0, { openByDefault: true }),
  ];

  if (type) {
    const layout = DETAIL_LAYOUTS[type];
    const filters = new Map(attributeFiltersFor(type).map((f) => [f.field, f]));
    for (const detail of layout.sections) {
      // The form's rental-only "Reguli de închiriere" is covered by the rental terms section below.
      if (detail.rentalFields) continue;

      if (isProximitySection(detail)) {
        if (proximities.length > 0) sections.push(section("proximities", countSelected(state.proximityIds, proximities)));
        continue;
      }

      if (isAmenitySection(detail)) {
        const items = amenitiesForSection(layout, detail, amenities, type);
        if (items.length > 0) sections.push(amenitySection(state, detail.id, items));
        continue;
      }

      const items: FilterItem[] = [
        ...detail.coreFields.flatMap((core): FilterItem[] => {
          if (core === "totalAreaM2") return [{ kind: "area" }];
          if (core === "yearBuilt" && yearBuiltApplies(type)) return [{ kind: "yearBuilt" }];
          if (core === "condition" && conditionApplies(type)) return [{ kind: "condition" }];
          return [];
        }),
        ...detail.attributeFields.flatMap((name): FilterItem[] => {
          const filter = filters.get(name);
          return filter && attributeFilterVisible(filter, state) ? [{ kind: "attribute", filter }] : [];
        }),
      ];
      // Collapsed, so the whole list of sections is visible at a glance.
      if (items.length > 0) sections.push(itemsSection(state, detail.id, items));
    }
  } else {
    sections.push(itemsSection(state, "area", [{ kind: "area" }]));
    if (amenities.length > 0) sections.push(amenitySection(state, "amenities", amenities));
    if (proximities.length > 0) sections.push(section("proximities", countSelected(state.proximityIds, proximities)));
  }

  if (rentalFiltersApply(state)) {
    sections.push(section("rentalTerms", RENTAL_PARAMS.filter((p) => has(state, p)).length));
  }

  return sections;
}

function countSelected(selected: readonly string[] | undefined, items: readonly { id: string }[]): number {
  const ids = new Set(selected ?? []);
  return items.filter((i) => ids.has(i.id)).length;
}
