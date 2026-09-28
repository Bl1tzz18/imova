import { describe, expect, it } from "vitest";
import { appliedDetailFilters, formatRange } from "@/lib/search/applied";
import { activeFilterCount, clearAllFilters, clearDetailFilters, detailFilterCount, parseSearchParams, updateSearch } from "@/lib/search/filters";

const AMENITY_A = "a1000000-0000-0000-0000-000000000002";
const AMENITY_B = "a1000000-0000-0000-0000-000000000003";
const RAION = "042ba2c8-22d8-498c-9638-06e5d1f21b8f";

describe("appliedDetailFilters", () => {
  const state = parseSearchParams({
    transactionType: "Rent",
    propertyType: "Apartment",
    raionId: RAION,
    minPriceEur: "300",
    minAreaM2: "40",
    minYearBuilt: "2000",
    maxYearBuilt: "2010",
    minRooms: "2",
    maxRooms: "3",
    layout: "Studio",
    gasSupply: "true",
    petsAllowed: "false",
    amenityIds: [AMENITY_A, AMENITY_B],
  });

  it("lists the drawer's filters in its order — never the bar's (transaction, type, location, price)", () => {
    expect(appliedDetailFilters(state).map((f) => f.id)).toEqual([
      "area", "yearBuilt", "layout", "rooms", "gasSupply", "petsAllowed", `amenity:${AMENITY_A}`, `amenity:${AMENITY_B}`,
    ]);
  });

  it("describes each as a range or a value", () => {
    const byId = Object.fromEntries(appliedDetailFilters(state).map((f) => [f.id, f]));
    expect(byId.area).toMatchObject({ kind: "range", min: "40", max: null });
    expect(byId.rooms).toMatchObject({ kind: "range", min: "2", max: "3" });
    expect(byId.layout).toMatchObject({ kind: "value", value: "Studio" });
    expect(byId.rooms.kind === "range" && byId.rooms.filter?.field).toBe("rooms");
  });

  it("removes exactly its own filter — one amenity leaves the others", () => {
    const byId = Object.fromEntries(appliedDetailFilters(state).map((f) => [f.id, f]));
    expect(updateSearch(state, byId.rooms.remove)).not.toHaveProperty("minRooms");
    expect(updateSearch(state, byId.rooms.remove)).not.toHaveProperty("maxRooms");
    expect(updateSearch(state, byId[`amenity:${AMENITY_A}`].remove).amenityIds).toEqual([AMENITY_B]);
  });

  it("is empty with only bar filters set", () => {
    expect(appliedDetailFilters(parseSearchParams({ transactionType: "Sale", propertyType: "House", minPriceEur: "1" }))).toEqual([]);
  });

  it("all go with clearDetailFilters, which keeps the bar's filters and the sort", () => {
    const sorted = updateSearch(state, { sort: "PriceAsc" });
    expect(updateSearch(sorted, clearDetailFilters(sorted))).toEqual({
      transactionType: ["Rent"], propertyType: ["Apartment"], raionId: [RAION], minPriceEur: ["300"], sort: ["PriceAsc"],
    });
  });

  it("clearAllFilters drops every filter, bar ones included, and keeps the sort", () => {
    const sorted = updateSearch(state, { sort: "PriceAsc", page: "3" });
    const reset = updateSearch(sorted, clearAllFilters(sorted));
    expect(reset).toEqual({ sort: ["PriceAsc"] });
    expect(activeFilterCount(reset)).toBe(0);
  });

  it("matches the drawer's badge count", () => {
    expect(detailFilterCount(state)).toBe(appliedDetailFilters(state).length);
  });
});

describe("formatRange", () => {
  const words = { from: "de la", upTo: "până la" };

  it("reads both ends, one end, or a single value", () => {
    expect(formatRange("2", "3", words)).toBe("2–3");
    expect(formatRange("2", null, words)).toBe("de la 2");
    expect(formatRange(null, "3", words)).toBe("până la 3");
    expect(formatRange("2", "2", words)).toBe("2");
    expect(formatRange(null, null, words)).toBe("");
  });

  it("formats numbers with the caller's formatter", () => {
    expect(formatRange("50000", "90000", words, (n) => Number(n).toLocaleString("ro-RO"))).toBe("50.000–90.000");
  });
});
