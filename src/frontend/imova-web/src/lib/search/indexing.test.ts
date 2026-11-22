import { describe, expect, it } from "vitest";
import { searchIndexing } from "./indexing";

const RAION = "0e2a8c34-1b2c-4d5e-8f90-123456789abc";

describe("searchIndexing", () => {
  it("indexes what people search for: a transaction, a type, a place", () => {
    expect(searchIndexing({})).toEqual({ indexable: true, path: "/search" });
    expect(searchIndexing({ transactionType: ["Rent"], propertyType: ["Apartment"], raionId: [RAION] })).toEqual({
      indexable: true,
      path: `/search?transactionType=Rent&propertyType=Apartment&raionId=${RAION}`,
    });
  });

  it("leaves out narrower slices, other orders and later pages", () => {
    expect(searchIndexing({ transactionType: ["Sale"], maxPriceEur: ["90000"] })).toEqual({ indexable: false });
    expect(searchIndexing({ transactionType: ["Sale"], sort: ["PriceAsc"] })).toEqual({ indexable: false });
    expect(searchIndexing({ transactionType: ["Sale"], page: ["2"] })).toEqual({ indexable: false });
  });
});
