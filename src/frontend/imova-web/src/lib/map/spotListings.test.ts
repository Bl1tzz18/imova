import { describe, expect, it } from "vitest";
import type { Listing } from "@/types/listing";
import { availableSorts, priceRange, sharedStreetAddress, sortSpotListings } from "./spotListings";

const listing = (
  id: string,
  {
    priceEur = 100_000,
    amount = priceEur,
    currency = "EUR",
    area = 60,
    floor,
    street = "str. Durlești",
    buildingNumber = "20/D",
  }: {
    priceEur?: number;
    amount?: number;
    currency?: string;
    area?: number;
    floor?: number;
    street?: string | null;
    buildingNumber?: string | null;
  } = {},
) =>
  ({
    id,
    price: { amount, currency, priceEur },
    property: {
      totalAreaM2: area,
      typeSpecificAttributes: floor == null ? {} : { floor },
      location: { street, buildingNumber },
    },
  }) as unknown as Listing;

describe("sharedStreetAddress", () => {
  it("gives street and number when every listing agrees", () => {
    expect(sharedStreetAddress([listing("a"), listing("b", { street: " STR. Durlești ", buildingNumber: "20/d" })])).toBe(
      "str. Durlești 20/D",
    );
  });

  it("drops the number when the listings only share the street", () => {
    expect(sharedStreetAddress([listing("a"), listing("b", { buildingNumber: "22" })])).toBe("str. Durlești");
    expect(sharedStreetAddress([listing("a"), listing("b", { buildingNumber: null })])).toBe("str. Durlești");
  });

  it("is null when the streets differ or nobody gave one", () => {
    expect(sharedStreetAddress([listing("a"), listing("b", { street: "bd. Dacia" })])).toBeNull();
    expect(sharedStreetAddress([listing("a", { street: null }), listing("b", { street: null })])).toBeNull();
    expect(sharedStreetAddress([])).toBeNull();
  });
});

describe("priceRange", () => {
  it("compares in EUR but keeps each listing's own price", () => {
    const range = priceRange([
      listing("a", { priceEur: 90_000 }),
      listing("b", { priceEur: 51_000, amount: 1_000_000, currency: "MDL" }),
      listing("c", { priceEur: 140_000 }),
    ]);
    expect(range).toEqual({
      min: { amount: 1_000_000, currency: "MDL", priceEur: 51_000 },
      max: { amount: 140_000, currency: "EUR", priceEur: 140_000 },
    });
  });

  it("is null for a single listing or when all cost the same", () => {
    expect(priceRange([listing("a")])).toBeNull();
    expect(priceRange([listing("a"), listing("b")])).toBeNull();
  });
});

describe("availableSorts", () => {
  it("offers area and floor only when they tell the listings apart", () => {
    expect(availableSorts([listing("a"), listing("b")])).toEqual(["price"]);
    expect(availableSorts([listing("a", { area: 50, floor: 2 }), listing("b", { area: 80, floor: 7 })])).toEqual([
      "price",
      "area",
      "floor",
    ]);
    expect(availableSorts([listing("a", { floor: 2 }), listing("b", { floor: 2 })])).toEqual(["price"]);
    expect(availableSorts([listing("a", { floor: 2 }), listing("b")])).toEqual(["price"]);
  });
});

describe("sortSpotListings", () => {
  const spot = [
    listing("a", { priceEur: 120_000, area: 70, floor: 5 }),
    listing("b", { priceEur: 80_000, area: 45 }),
    listing("c", { priceEur: 95_000, area: 90, floor: 2 }),
    listing("d", { priceEur: 80_000, area: 45, floor: 9 }),
  ];
  const ids = (ls: Listing[]) => ls.map((l) => l.id);

  it("sorts cheapest first, ties by id", () => {
    expect(ids(sortSpotListings(spot, "price"))).toEqual(["b", "d", "c", "a"]);
  });

  it("sorts biggest first", () => {
    expect(ids(sortSpotListings(spot, "area"))).toEqual(["c", "a", "b", "d"]);
  });

  it("sorts lowest floor first, listings without a floor last", () => {
    expect(ids(sortSpotListings(spot, "floor"))).toEqual(["c", "a", "d", "b"]);
  });

  it("doesn't change the list it was given", () => {
    sortSpotListings(spot, "price");
    expect(ids(spot)).toEqual(["a", "b", "c", "d"]);
  });
});
