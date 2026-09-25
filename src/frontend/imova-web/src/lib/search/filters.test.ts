import { describe, expect, it } from "vitest";
import {
  activeFilterCount,
  applicableTypeFilters,
  currentPage,
  parseSearchParams,
  searchHref,
  toQueryString,
  updateSearch,
} from "@/lib/search/filters";

const RAION = "042ba2c8-22d8-498c-9638-06e5d1f21b8f";
const AMENITY = "a1000000-0000-0000-0000-000000000002";

describe("parseSearchParams", () => {
  it("reads known filters, including repeated ones", () => {
    const state = parseSearchParams(
      new URLSearchParams(`transactionType=Rent&propertyType=Apartment&propertyType=House&raionId=${RAION}&minPriceEur=100&amenityIds=${AMENITY}`),
    );

    expect(state).toEqual({
      transactionType: ["Rent"],
      propertyType: ["Apartment", "House"],
      raionId: [RAION],
      minPriceEur: ["100"],
      amenityIds: [AMENITY],
    });
  });

  it("accepts Next.js searchParams objects too", () => {
    expect(parseSearchParams({ propertyType: ["Garage", "Room"], sort: "PriceAsc" })).toEqual({
      propertyType: ["Garage", "Room"],
      sort: ["PriceAsc"],
    });
  });

  it("drops unknown parameters and malformed values instead of failing", () => {
    expect(
      parseSearchParams({
        propertyType: ["Castle", "Land"],
        transactionType: "Lease",
        raionId: "not-a-guid",
        minPriceEur: "-5",
        maxAreaM2: "abc",
        utm_source: "newsletter",
      }),
    ).toEqual({ propertyType: ["Land"] });
  });

  it("keeps a single value for single-value filters and de-duplicates repeats", () => {
    expect(parseSearchParams(new URLSearchParams("sort=PriceAsc&sort=PriceDesc&propertyType=Room&propertyType=Room"))).toEqual({
      sort: ["PriceAsc"],
      propertyType: ["Room"],
    });
  });

  it("drops the defaults (page 1, newest first)", () => {
    expect(parseSearchParams({ page: "1", sort: "Newest" })).toEqual({});
  });
});

describe("type-specific filters", () => {
  it("apply only with exactly one property type they belong to", () => {
    expect(parseSearchParams({ propertyType: "Apartment", minRooms: "2" })).toEqual({ propertyType: ["Apartment"], minRooms: ["2"] });
    expect(parseSearchParams({ propertyType: ["Apartment", "House"], minRooms: "2" })).toEqual({ propertyType: ["Apartment", "House"] });
    expect(parseSearchParams({ propertyType: "Land", minRooms: "2" })).toEqual({ propertyType: ["Land"] });
    expect(parseSearchParams({ minRooms: "2" })).toEqual({});
  });

  it("keep only enum values that are options for that type", () => {
    expect(parseSearchParams({ propertyType: "Land", plotType: ["Forest", "Villa"] })).toEqual({ propertyType: ["Land"], plotType: ["Forest"] });
  });

  it("are listed per type for the filter panel", () => {
    const fields = (type: string) => applicableTypeFilters({ propertyType: [type] }).map((f) => f.field);
    expect(fields("Apartment")).toEqual(["rooms", "floor", "bathrooms", "housingStockType", "layout", "heatingSystem"]);
    expect(fields("House")).toEqual(["rooms", "landAreaM2", "heatingSystem", "houseType"]);
    expect(fields("Land")).toEqual(["plotType", "locationContext", "roadAccess"]);
    expect(fields("Commercial")).toEqual(["floor", "spaceType"]);
    expect(fields("Garage")).toEqual(["parkingType"]);
    expect(fields("Room")).toEqual(["bathroomType"]);
    expect(applicableTypeFilters({ propertyType: ["Apartment", "House"] })).toEqual([]);
  });
});

describe("rental filters", () => {
  it("apply only to a rental search", () => {
    expect(parseSearchParams({ transactionType: "Rent", petsAllowed: "true" })).toEqual({ transactionType: ["Rent"], petsAllowed: ["true"] });
    expect(parseSearchParams({ transactionType: "Sale", petsAllowed: "true" })).toEqual({ transactionType: ["Sale"] });
    expect(parseSearchParams({ petsAllowed: "true", maxLeasePeriodMonths: "6" })).toEqual({});
  });
});

describe("updateSearch", () => {
  const apartmentRent = parseSearchParams({ transactionType: "Rent", propertyType: "Apartment", minRooms: "2", petsAllowed: "true", page: "3" });

  it("clears type-specific filters when the property type changes", () => {
    expect(updateSearch(apartmentRent, { propertyType: ["Apartment", "House"] })).toEqual({
      transactionType: ["Rent"],
      propertyType: ["Apartment", "House"],
      petsAllowed: ["true"],
    });
  });

  it("clears rental filters when leaving rentals", () => {
    expect(updateSearch(apartmentRent, { transactionType: "Sale" })).toEqual({
      transactionType: ["Sale"],
      propertyType: ["Apartment"],
      minRooms: ["2"],
    });
  });

  it("goes back to page 1 on any filter change, but not when paging", () => {
    expect(updateSearch(apartmentRent, { minRooms: "3" }).page).toBeUndefined();
    expect(updateSearch(apartmentRent, { page: "4" }).page).toEqual(["4"]);
  });

  it("removes a filter set to null or empty", () => {
    expect(updateSearch(apartmentRent, { minRooms: null, petsAllowed: "" })).toEqual({
      transactionType: ["Rent"],
      propertyType: ["Apartment"],
    });
  });
});

describe("URLs", () => {
  it("round-trip in a canonical parameter order", () => {
    const state = parseSearchParams(new URLSearchParams(`sort=PriceAsc&minPriceEur=100&propertyType=House&transactionType=Sale&raionId=${RAION}`));
    const query = toQueryString(state);

    expect(query).toBe(`transactionType=Sale&propertyType=House&raionId=${RAION}&minPriceEur=100&sort=PriceAsc`);
    expect(parseSearchParams(new URLSearchParams(query))).toEqual(state);
  });

  it("point at /cauta", () => {
    expect(searchHref({})).toBe("/cauta");
    expect(searchHref({ propertyType: ["Apartment"] })).toBe("/cauta?propertyType=Apartment");
  });
});

describe("activeFilterCount / currentPage", () => {
  it("counts a min/max range and a location as one filter each, every multi-select value separately", () => {
    const state = parseSearchParams({
      propertyType: ["Apartment", "House"],
      minPriceEur: "100",
      maxPriceEur: "200",
      raionId: RAION,
      sort: "PriceAsc",
      page: "2",
    });

    expect(activeFilterCount(state)).toBe(4);
    expect(currentPage(state)).toBe(2);
    expect(currentPage({})).toBe(1);
  });
});
