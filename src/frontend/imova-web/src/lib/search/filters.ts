import {
  GENERAL_CONDITIONS,
  PROPERTY_TYPES,
  attributeSchemaFor,
  hasBuilding,
  isFieldVisible,
  usesGeneralCondition,
  type AttributeField,
  type PropertyTypeName,
} from "@/lib/property/attributeSchema";
import { petsApplyTo } from "@/lib/property/rentalFields";

// The /search page's whole state lives in its URL: one query parameter per filter, named exactly
// like the backend's GET /api/v1/listings/search parameters (multi-value filters repeat the name).
// Everything that leads to search — the hero, category tiles, the Cumpără/Închiriază links and the
// filter panel itself — builds its URL through this module, so there's one set of rules for which
// filters apply when.

export const SEARCH_PATH = "/search";
// The same search on a map (every matching listing with coordinates, as pins).
export const MAP_PATH = "/map";

export type SearchView = "list" | "map";

export const TRANSACTION_TYPES = ["Sale", "Rent"] as const;
export type TransactionTypeName = (typeof TRANSACTION_TYPES)[number];

export const SORTS = ["Newest", "PriceAsc", "PriceDesc", "AreaDesc"] as const;
export type SortName = (typeof SORTS)[number];

// A filter on one TypeSpecificAttributes field — every field the listing form asks for the type
// (ATTRIBUTE_SCHEMA) except free text. Named like the backend's AttributeFilterParser: a number
// field is a min<Field>/max<Field> range, an enum field and a yes/no field take the field's name.
// Only offered (and kept in the URL) while its property type is the selected one.
export type AttributeFilter = {
  field: string;
  kind: "range" | "choice" | "yesno";
  params: readonly string[];
  schema: AttributeField;
};

const capitalize = (name: string) => name[0].toUpperCase() + name.slice(1);

function toFilter(field: AttributeField): AttributeFilter | null {
  switch (field.kind) {
    case "int":
    case "decimal":
      return { field: field.name, kind: "range", params: [`min${capitalize(field.name)}`, `max${capitalize(field.name)}`], schema: field };
    case "enum":
      return { field: field.name, kind: "choice", params: [field.name], schema: field };
    case "yesno":
      return { field: field.name, kind: "yesno", params: [field.name], schema: field };
    default:
      return null;
  }
}

// The attribute filters of a property type, in the listing form's order.
export function attributeFiltersFor(type: string): AttributeFilter[] {
  return attributeSchemaFor(type).flatMap((field) => toFilter(field) ?? []);
}

// Every attribute filter parameter of any type → the filters it stands for (a field such as
// "floor" exists on several types).
const FILTERS_BY_PARAM = new Map<string, AttributeFilter[]>();
for (const type of PROPERTY_TYPES) {
  for (const filter of attributeFiltersFor(type)) {
    for (const param of filter.params) FILTERS_BY_PARAM.set(param, [...(FILTERS_BY_PARAM.get(param) ?? []), filter]);
  }
}

// A conditional field (heating energy source, soil score, office count) only applies while its
// controlling filter is set to a value that shows it on the form.
export function attributeFilterVisible(filter: AttributeFilter, state: SearchState): boolean {
  return isFieldVisible(filter.schema, (name) => state[name.replace(/^attr\./, "")]?.[0] ?? null);
}

// General Property fields the form asks for some types: year built (anything with a building) and
// the general condition (Garage and Room only).
export const YEAR_BUILT_PARAMS = ["minYearBuilt", "maxYearBuilt"] as const;
export const CONDITION_PARAM = "condition";

export function yearBuiltApplies(type: string | null): boolean {
  return type !== null && hasBuilding(type);
}

export function conditionApplies(type: string | null): boolean {
  return type !== null && usesGeneralCondition(type);
}

// Rental terms — only for rentals (the backend rejects them together with transactionType=Sale).
export const RENTAL_PARAMS = ["petsAllowed", "utilitiesIncluded", "maxLeasePeriodMonths"] as const;

// Everything else holds one value — the property type too: the panel's filters follow that type's
// listing form, so there's always exactly one or none.
const MULTI_PARAMS = new Set(["amenityIds", "proximityIds"]);

// Every parameter, in the order it's written to the URL (a canonical order keeps equal searches on
// equal URLs).
const PARAM_ORDER = [
  "transactionType",
  "propertyType",
  "raionId",
  "localitateId",
  "chisinauSectorId",
  "minPriceEur",
  "maxPriceEur",
  "minAreaM2",
  "maxAreaM2",
  ...YEAR_BUILT_PARAMS,
  CONDITION_PARAM,
  ...FILTERS_BY_PARAM.keys(),
  ...RENTAL_PARAMS,
  "amenityIds",
  "proximityIds",
  "sort",
  "page",
] as const;

// Parameters -> their value(s). Only known, well-formed values ever get in.
export type SearchState = Record<string, string[]>;

export type RawSearchParams = Record<string, string | string[] | undefined> | URLSearchParams;

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const NUMBER = /^-?\d+(\.\d+)?$/;

function isValidValue(param: string, value: string): boolean {
  switch (param) {
    case "transactionType":
      return (TRANSACTION_TYPES as readonly string[]).includes(value);
    case "propertyType":
      return (PROPERTY_TYPES as readonly string[]).includes(value);
    case "sort":
      return (SORTS as readonly string[]).includes(value);
    case "page":
      return /^[1-9]\d{0,4}$/.test(value);
    case "raionId":
    case "localitateId":
    case "chisinauSectorId":
    case "amenityIds":
    case "proximityIds":
      return GUID.test(value);
    case "petsAllowed":
    case "utilitiesIncluded":
      return value === "true" || value === "false";
    case CONDITION_PARAM:
      return (GENERAL_CONDITIONS as readonly string[]).includes(value);
  }
  const filters = FILTERS_BY_PARAM.get(param);
  if (filters?.[0].kind === "choice") return filters.some((f) => "options" in f.schema && f.schema.options.includes(value));
  if (filters?.[0].kind === "yesno") return value === "true" || value === "false";
  // Numbers: negative only where the form allows it (floor: -1 = basement).
  const negativeAllowed = filters?.some((f) => "min" in f.schema && f.schema.min < 0) ?? false;
  return NUMBER.test(value) && (negativeAllowed || !value.startsWith("-"));
}

function entries(raw: RawSearchParams): [string, string][] {
  if (raw instanceof URLSearchParams) return [...raw.entries()];
  return Object.entries(raw).flatMap(([key, value]) =>
    value === undefined ? [] : (Array.isArray(value) ? value : [value]).map((v) => [key, v] as [string, string]),
  );
}

// URL -> state: unknown parameters and malformed values are dropped, then the rules below applied —
// so a hand-edited or outdated URL still shows results instead of an error.
export function parseSearchParams(raw: RawSearchParams): SearchState {
  const state: SearchState = {};
  for (const [key, value] of entries(raw)) {
    if (!(PARAM_ORDER as readonly string[]).includes(key) || !isValidValue(key, value)) continue;
    const current = state[key] ?? [];
    // A repeated single-value parameter keeps its first value (the API would reject two).
    if (current.includes(value) || (!MULTI_PARAMS.has(key) && current.length > 0)) continue;
    state[key] = [...current, value];
  }
  return sanitize(state);
}

// The selected property type, if any.
export function singlePropertyType(state: SearchState): PropertyTypeName | null {
  return (state.propertyType?.[0] as PropertyTypeName | undefined) ?? null;
}

export function rentalFiltersApply(state: SearchState): boolean {
  return state.transactionType?.[0] === "Rent";
}

// Pets are a rental question only for somewhere people live (see petsApplyTo).
export function petsFilterApplies(state: SearchState): boolean {
  const type = singlePropertyType(state);
  return rentalFiltersApply(state) && (type === null || petsApplyTo(type));
}

// Drops whatever no longer applies: attribute filters of another type (or of a conditional field
// whose controlling value is gone, or with a value that isn't an option for the type), year
// built / condition where the type has none, rental terms off a rental search, empty values, the
// defaults (page 1, newest first).
function sanitize(state: SearchState): SearchState {
  const type = singlePropertyType(state);
  const offered = new Map(attributeFiltersFor(type ?? "").flatMap((f) => f.params.map((p) => [p, f] as const)));
  const next: SearchState = {};
  for (const [key, values] of Object.entries(state)) {
    let kept = values.filter((v) => v !== "");
    if (FILTERS_BY_PARAM.has(key)) {
      const filter = offered.get(key);
      if (!filter || !attributeFilterVisible(filter, state)) continue;
      if (filter.kind === "choice" && "options" in filter.schema) {
        const options: readonly string[] = filter.schema.options;
        kept = kept.filter((v) => options.includes(v));
      }
    }
    if ((YEAR_BUILT_PARAMS as readonly string[]).includes(key) && !yearBuiltApplies(type)) continue;
    if (key === CONDITION_PARAM && !conditionApplies(type)) continue;
    if ((RENTAL_PARAMS as readonly string[]).includes(key) && !rentalFiltersApply(state)) continue;
    if (key === "petsAllowed" && !petsFilterApplies(state)) continue;
    if (key === "page" && kept[0] === "1") continue;
    if (key === "sort" && kept[0] === "Newest") continue;
    if (kept.length > 0) next[key] = kept;
  }
  return next;
}

// A change from the filter panel: set (or with [] / null, clear) some parameters. Any change other
// than to the page itself goes back to page 1; the rules then drop what no longer applies.
export function updateSearch(state: SearchState, changes: Record<string, string | string[] | null>): SearchState {
  const next: SearchState = { ...state };
  for (const [key, value] of Object.entries(changes)) {
    const values = value === null ? [] : Array.isArray(value) ? value : [value];
    if (values.length === 0 || values.every((v) => v === "")) delete next[key];
    else next[key] = values;
  }
  if (!("page" in changes)) delete next.page;
  return sanitize(next);
}

export function toQueryString(state: SearchState): string {
  const params = new URLSearchParams();
  for (const key of PARAM_ORDER) {
    for (const value of state[key] ?? []) params.append(key, value);
  }
  return params.toString();
}

export function searchHref(state: SearchState, view: SearchView = "list"): string {
  const path = view === "map" ? MAP_PATH : SEARCH_PATH;
  const query = toQueryString(state);
  return query ? `${path}?${query}` : path;
}

// The same search in the other view: every filter and the sort carry over; the page number only
// means something in the list (the map shows every pin at once), so it's dropped.
export function switchViewHref(state: SearchState, view: SearchView): string {
  const rest = Object.fromEntries(Object.entries(state).filter(([key]) => key !== "page"));
  return searchHref(rest, view);
}

// How many filters are active (sort and page aren't filters) — the mobile "Filtre (n)" badge.
export function activeFilterCount(state: SearchState): number {
  const counted = new Set<string>();
  for (const [key, values] of Object.entries(state)) {
    if (key === "sort" || key === "page") continue;
    const range = /^(?:min|max)([A-Z].*)$/.exec(key);
    if (range) counted.add(`range:${range[1]}`);
    else if (key === "raionId" || key === "localitateId" || key === "chisinauSectorId") counted.add("location");
    else if (MULTI_PARAMS.has(key)) values.forEach((v) => counted.add(`${key}:${v}`));
    else counted.add(key);
  }
  return counted.size;
}

// The filters the search page's top bar shows directly (transaction, type, location, price); the
// rest live in the "Mai multe filtre" drawer.
export const BAR_PARAMS = [
  "transactionType",
  "propertyType",
  "raionId",
  "localitateId",
  "chisinauSectorId",
  "minPriceEur",
  "maxPriceEur",
] as const;

// How many of the drawer's filters are active — the "Mai multe filtre (n)" badge.
export function detailFilterCount(state: SearchState): number {
  const rest = Object.fromEntries(Object.entries(state).filter(([key]) => !(BAR_PARAMS as readonly string[]).includes(key)));
  return activeFilterCount(rest);
}

// The change that clears every drawer filter, leaving the bar's filters and the sort order.
export function clearDetailFilters(state: SearchState): Record<string, null> {
  const kept = new Set<string>([...BAR_PARAMS, "sort"]);
  return Object.fromEntries(Object.keys(state).filter((key) => !kept.has(key)).map((key) => [key, null]));
}

// The change that clears every filter — the bar's "Resetează". Sorting isn't a filter, so it stays.
export function clearAllFilters(state: SearchState): Record<string, null> {
  return Object.fromEntries(Object.keys(state).filter((key) => key !== "sort").map((key) => [key, null]));
}

export function currentPage(state: SearchState): number {
  return Number(state.page?.[0] ?? 1);
}
