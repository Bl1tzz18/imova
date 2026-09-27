import { describe, expect, it } from "vitest";
import {
  activeFilterCount,
  attributeFiltersFor,
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
      new URLSearchParams(`transactionType=Rent&propertyType=Apartment&raionId=${RAION}&minPriceEur=100&amenityIds=${AMENITY}`),
    );

    expect(state).toEqual({
      transactionType: ["Rent"],
      propertyType: ["Apartment"],
      raionId: [RAION],
      minPriceEur: ["100"],
      amenityIds: [AMENITY],
    });
  });

  it("accepts Next.js searchParams objects too", () => {
    expect(parseSearchParams({ propertyType: "Garage", sort: "PriceAsc" })).toEqual({
      propertyType: ["Garage"],
      sort: ["PriceAsc"],
    });
  });

  it("keeps a single property type — the first one", () => {
    expect(parseSearchParams({ propertyType: ["Garage", "Room"] })).toEqual({ propertyType: ["Garage"] });
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

describe("attribute filters", () => {
  it("are every non-text field of the type's listing form, in its order", () => {
    const params = (type: string) => attributeFiltersFor(type).flatMap((f) => f.params);
    expect(params("Garage")).toEqual(["parkingType"]);
    expect(params("Room")).toEqual(["bathroomType", "minRoommateCount", "maxRoommateCount"]);
    expect(attributeFiltersFor("Apartment").map((f) => f.field)).toEqual([
      "housingStockType", "buildingMaterial", "finishCondition", "layout", "rooms", "floor", "totalFloors", "bathrooms",
      "livingAreaM2", "kitchenAreaM2", "heatingSystem", "heatingEnergySource", "heatingDistribution", "gasSupply", "floorMaterial",
    ]);
    // Free text (a commercial space's electrical power) isn't filterable.
    expect(attributeFiltersFor("Commercial").map((f) => f.field)).not.toContain("electricalPower");
    expect(attributeFiltersFor("Commercial").find((f) => f.field === "mainStreetAccess")?.kind).toBe("yesno");
  });

  it("apply only with their property type selected", () => {
    expect(parseSearchParams({ propertyType: "Apartment", minRooms: "2", gasSupply: "true" })).toEqual({
      propertyType: ["Apartment"], minRooms: ["2"], gasSupply: ["true"],
    });
    expect(parseSearchParams({ propertyType: "Land", minRooms: "2" })).toEqual({ propertyType: ["Land"] });
    expect(parseSearchParams({ minRooms: "2" })).toEqual({});
  });

  it("keep only values that are options for the type", () => {
    expect(parseSearchParams({ propertyType: "Land", plotType: "Villa" })).toEqual({ propertyType: ["Land"] });
    expect(parseSearchParams({ propertyType: "Apartment", gasSupply: "maybe" })).toEqual({ propertyType: ["Apartment"] });
  });

  it("allow negative numbers only where the form does (floor)", () => {
    expect(parseSearchParams({ propertyType: "Commercial", minFloor: "-1" })).toEqual({ propertyType: ["Commercial"], minFloor: ["-1"] });
    expect(parseSearchParams({ propertyType: "Apartment", minRooms: "-1" })).toEqual({ propertyType: ["Apartment"] });
  });

  it("keep a conditional field only while its controlling filter shows it", () => {
    expect(parseSearchParams({ propertyType: "House", heatingSystem: "OwnBoiler", heatingEnergySource: "Gas" })).toEqual({
      propertyType: ["House"], heatingSystem: ["OwnBoiler"], heatingEnergySource: ["Gas"],
    });
    expect(parseSearchParams({ propertyType: "House", heatingSystem: "DistrictHeating", heatingEnergySource: "Gas" })).toEqual({
      propertyType: ["House"], heatingSystem: ["DistrictHeating"],
    });
    expect(parseSearchParams({ propertyType: "Land", minSoilQualityScore: "50" })).toEqual({ propertyType: ["Land"] });
  });

  it("year built and condition follow the form: a building, and the general condition only for Garage/Room", () => {
    expect(parseSearchParams({ propertyType: "Garage", minYearBuilt: "2000", condition: "New" })).toEqual({
      propertyType: ["Garage"], minYearBuilt: ["2000"], condition: ["New"],
    });
    expect(parseSearchParams({ propertyType: "Land", minYearBuilt: "2000" })).toEqual({ propertyType: ["Land"] });
    expect(parseSearchParams({ propertyType: "Apartment", condition: "New" })).toEqual({ propertyType: ["Apartment"] });
    expect(parseSearchParams({ minYearBuilt: "2000" })).toEqual({});
  });
});

describe("rental filters", () => {
  it("apply only to a rental search", () => {
    expect(parseSearchParams({ transactionType: "Rent", petsAllowed: "true" })).toEqual({ transactionType: ["Rent"], petsAllowed: ["true"] });
    expect(parseSearchParams({ transactionType: "Sale", petsAllowed: "true" })).toEqual({ transactionType: ["Sale"] });
    expect(parseSearchParams({ petsAllowed: "true", maxLeasePeriodMonths: "6" })).toEqual({});
  });

  it("ask about pets only for somewhere people live", () => {
    expect(parseSearchParams({ transactionType: "Rent", propertyType: "Garage", petsAllowed: "true", utilitiesIncluded: "true" })).toEqual({
      transactionType: ["Rent"], propertyType: ["Garage"], utilitiesIncluded: ["true"],
    });
  });
});

describe("updateSearch", () => {
  const apartmentRent = parseSearchParams({ transactionType: "Rent", propertyType: "Apartment", minRooms: "2", petsAllowed: "true", page: "3" });

  it("clears the old type's attribute filters when the property type changes", () => {
    expect(updateSearch(apartmentRent, { propertyType: "Land" })).toEqual({
      transactionType: ["Rent"],
      propertyType: ["Land"],
    });
    // Rooms mean the same for a house, so they stay.
    expect(updateSearch(apartmentRent, { propertyType: "House" })).toMatchObject({ propertyType: ["House"], minRooms: ["2"] });
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

  it("point at /search", () => {
    expect(searchHref({})).toBe("/search");
    expect(searchHref({ propertyType: ["Apartment"] })).toBe("/search?propertyType=Apartment");
  });
});

describe("activeFilterCount / currentPage", () => {
  it("counts a min/max range and a location as one filter each, every multi-select value separately", () => {
    const state = parseSearchParams({
      propertyType: "Apartment",
      minPriceEur: "100",
      maxPriceEur: "200",
      minRooms: "2",
      maxRooms: "3",
      raionId: RAION,
      amenityIds: [AMENITY, "a1000000-0000-0000-0000-000000000003"],
      sort: "PriceAsc",
      page: "2",
    });

    expect(activeFilterCount(state)).toBe(6);
    expect(currentPage(state)).toBe(2);
    expect(currentPage({})).toBe(1);
  });
});
