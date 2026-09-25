import { ATTRIBUTE_SCHEMA, PROPERTY_TYPES, type PropertyTypeName } from "@/lib/property/attributeSchema";

// The /cauta page's whole state lives in its URL: one query parameter per filter, named exactly
// like the backend's GET /api/v1/listings/search parameters (multi-value filters repeat the name).
// Everything that leads to search — the hero, category tiles, the Cumpără/Închiriază links and the
// filter panel itself — builds its URL through this module, so there's one set of rules for which
// filters apply when.

export const SEARCH_PATH = "/cauta";

export const TRANSACTION_TYPES = ["Sale", "Rent"] as const;
export type TransactionTypeName = (typeof TRANSACTION_TYPES)[number];

export const SORTS = ["Newest", "PriceAsc", "PriceDesc", "AreaDesc"] as const;
export type SortName = (typeof SORTS)[number];

// Type-specific filters, and the property types each belongs to — the same table as the backend's
// SearchFilterRules. A filter is only offered (and kept in the URL) while exactly one of its types
// is selected: "rooms" means nothing for a plot of land.
type TypeSpecificFilter = {
  field: string;
  types: readonly PropertyTypeName[];
  params: readonly string[];
  kind: "range" | "min" | "enum";
};

export const TYPE_SPECIFIC_FILTERS: readonly TypeSpecificFilter[] = [
  { field: "rooms", types: ["Apartment", "House"], params: ["minRooms", "maxRooms"], kind: "range" },
  { field: "floor", types: ["Apartment", "Commercial"], params: ["minFloor", "maxFloor"], kind: "range" },
  { field: "bathrooms", types: ["Apartment"], params: ["minBathrooms"], kind: "min" },
  { field: "landAreaM2", types: ["House"], params: ["minLandAreaM2", "maxLandAreaM2"], kind: "range" },
  { field: "housingStockType", types: ["Apartment"], params: ["housingStockType"], kind: "enum" },
  { field: "layout", types: ["Apartment"], params: ["layout"], kind: "enum" },
  { field: "heatingSystem", types: ["Apartment", "House"], params: ["heatingSystem"], kind: "enum" },
  { field: "houseType", types: ["House"], params: ["houseType"], kind: "enum" },
  { field: "plotType", types: ["Land"], params: ["plotType"], kind: "enum" },
  { field: "locationContext", types: ["Land"], params: ["locationContext"], kind: "enum" },
  { field: "roadAccess", types: ["Land"], params: ["roadAccess"], kind: "enum" },
  { field: "spaceType", types: ["Commercial"], params: ["spaceType"], kind: "enum" },
  { field: "parkingType", types: ["Garage"], params: ["parkingType"], kind: "enum" },
  { field: "bathroomType", types: ["Room"], params: ["bathroomType"], kind: "enum" },
];

// Rental terms — only for rentals (the backend rejects them together with transactionType=Sale).
export const RENTAL_PARAMS = ["petsAllowed", "utilitiesIncluded", "maxLeasePeriodMonths"] as const;

const MULTI_PARAMS = new Set([
  "propertyType",
  "amenityIds",
  "proximityIds",
  ...TYPE_SPECIFIC_FILTERS.filter((f) => f.kind === "enum").flatMap((f) => f.params),
]);

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
  ...TYPE_SPECIFIC_FILTERS.flatMap((f) => f.params),
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

function enumOptions(field: string, type: PropertyTypeName): readonly string[] {
  const schemaField = ATTRIBUTE_SCHEMA[type].find((f) => f.name === field);
  return schemaField && "options" in schemaField ? schemaField.options : [];
}

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
  }
  const typeFilter = TYPE_SPECIFIC_FILTERS.find((f) => f.params.includes(param));
  if (typeFilter?.kind === "enum") {
    return typeFilter.types.some((type) => enumOptions(typeFilter.field, type).includes(value));
  }
  return NUMBER.test(value) && (param.includes("Floor") || !value.startsWith("-"));
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

// The one property type type-specific filters apply to — null with none or several selected.
export function singlePropertyType(state: SearchState): PropertyTypeName | null {
  const types = state.propertyType ?? [];
  return types.length === 1 ? (types[0] as PropertyTypeName) : null;
}

export function applicableTypeFilters(state: SearchState): readonly TypeSpecificFilter[] {
  const type = singlePropertyType(state);
  return type ? TYPE_SPECIFIC_FILTERS.filter((f) => f.types.includes(type)) : [];
}

export function rentalFiltersApply(state: SearchState): boolean {
  return state.transactionType?.[0] === "Rent";
}

// Drops whatever no longer applies: type-specific filters without their one property type (or
// values that aren't options for it), rental terms off a rental search, an empty value, page 1.
function sanitize(state: SearchState): SearchState {
  const type = singlePropertyType(state);
  const allowed = new Set(applicableTypeFilters(state).flatMap((f) => f.params));
  const next: SearchState = {};
  for (const [key, values] of Object.entries(state)) {
    const typeFilter = TYPE_SPECIFIC_FILTERS.find((f) => f.params.includes(key));
    let kept = values.filter((v) => v !== "");
    if (typeFilter) {
      if (!allowed.has(key)) continue;
      if (typeFilter.kind === "enum" && type) kept = kept.filter((v) => enumOptions(typeFilter.field, type).includes(v));
    }
    if ((RENTAL_PARAMS as readonly string[]).includes(key) && !rentalFiltersApply(state)) continue;
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

export function searchHref(state: SearchState): string {
  const query = toQueryString(state);
  return query ? `${SEARCH_PATH}?${query}` : SEARCH_PATH;
}

// How many filters are active (sort and page aren't filters) — the mobile "Filtre (n)" badge.
export function activeFilterCount(state: SearchState): number {
  const ranges = new Map([
    ["minPriceEur", "price"], ["maxPriceEur", "price"], ["minAreaM2", "area"], ["maxAreaM2", "area"],
    ["minRooms", "rooms"], ["maxRooms", "rooms"], ["minFloor", "floor"], ["maxFloor", "floor"],
    ["minLandAreaM2", "landArea"], ["maxLandAreaM2", "landArea"],
    ["raionId", "location"], ["localitateId", "location"], ["chisinauSectorId", "location"],
  ]);
  const counted = new Set<string>();
  for (const [key, values] of Object.entries(state)) {
    if (key === "sort" || key === "page") continue;
    if (ranges.has(key)) counted.add(ranges.get(key)!);
    else if (MULTI_PARAMS.has(key)) values.forEach((v) => counted.add(`${key}:${v}`));
    else counted.add(key);
  }
  return counted.size;
}

export function currentPage(state: SearchState): number {
  return Number(state.page?.[0] ?? 1);
}
