import {
  CONDITION_PARAM,
  YEAR_BUILT_PARAMS,
  attributeFiltersFor,
  singlePropertyType,
  type AttributeFilter,
  type SearchState,
} from "./filters";

// The drawer filters currently applied, one chip each, in the drawer's order — shown under the
// search page's top bar, because once filters sit in a closed drawer the page must still say what
// narrows the results (and let each go with one click). `remove` is the change that drops it.
// Bar filters (transaction, type, location, price) aren't here: their pills already show them.
export type AppliedFilter = { id: string; remove: Record<string, string[] | null> } & (
  // A number range: the general area, year built, or an attribute (filter set).
  | { kind: "range"; field: "area" | "yearBuilt" | string; filter?: AttributeFilter; min: string | null; max: string | null }
  // One value: an attribute choice / yes-no (filter set), the general condition, a rental term.
  | { kind: "value"; field: string; filter?: AttributeFilter; value: string }
  | { kind: "amenity"; value: string }
  | { kind: "proximity"; value: string }
);

const first = (state: SearchState, param: string) => state[param]?.[0] ?? null;

function range(state: SearchState, field: string, params: readonly string[], filter?: AttributeFilter): AppliedFilter[] {
  const [min, max] = [first(state, params[0]), first(state, params[1])];
  if (min === null && max === null) return [];
  return [{ id: field, kind: "range", field, filter, min, max, remove: { [params[0]]: null, [params[1]]: null } }];
}

function value(state: SearchState, field: string, param: string, filter?: AttributeFilter): AppliedFilter[] {
  const v = first(state, param);
  return v === null ? [] : [{ id: field, kind: "value", field, filter, value: v, remove: { [param]: null } }];
}

export function appliedDetailFilters(state: SearchState): AppliedFilter[] {
  const type = singlePropertyType(state);
  const amenityIds = state.amenityIds ?? [];
  const proximityIds = state.proximityIds ?? [];
  return [
    ...range(state, "area", ["minAreaM2", "maxAreaM2"]),
    ...range(state, "yearBuilt", YEAR_BUILT_PARAMS),
    ...value(state, "condition", CONDITION_PARAM),
    ...(type ? attributeFiltersFor(type) : []).flatMap((filter) =>
      filter.kind === "range" ? range(state, filter.field, filter.params, filter) : value(state, filter.field, filter.params[0], filter),
    ),
    ...value(state, "petsAllowed", "petsAllowed"),
    ...value(state, "utilitiesIncluded", "utilitiesIncluded"),
    ...value(state, "maxLeasePeriodMonths", "maxLeasePeriodMonths"),
    ...amenityIds.map((id): AppliedFilter => ({
      id: `amenity:${id}`,
      kind: "amenity",
      value: id,
      remove: { amenityIds: amenityIds.filter((a) => a !== id) },
    })),
    ...proximityIds.map((id): AppliedFilter => ({
      id: `proximity:${id}`,
      kind: "proximity",
      value: id,
      remove: { proximityIds: proximityIds.filter((p) => p !== id) },
    })),
  ];
}

// "2–3", "de la 2", "până la 3" — how a range reads on a pill or chip. The words and the number
// format come from the caller (translations, the locale's thousands separator).
export function formatRange(
  min: string | null,
  max: string | null,
  words: { from: string; upTo: string },
  format: (n: string) => string = (n) => n,
): string {
  if (min !== null && max !== null) return min === max ? format(min) : `${format(min)}–${format(max)}`;
  if (min !== null) return `${words.from} ${format(min)}`;
  if (max !== null) return `${words.upTo} ${format(max)}`;
  return "";
}
