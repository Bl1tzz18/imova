import { describe, expect, it } from "vitest";
import { canGoBackTo, isSearchUrl, listingBackHref, recordNavigation } from "./history";

describe("isSearchUrl", () => {
  it("is the list or the map, with or without filters", () => {
    expect(isSearchUrl("/search")).toBe(true);
    expect(isSearchUrl("/search?transactionType=Rent&page=2")).toBe(true);
    expect(isSearchUrl("/map?propertyType=House")).toBe(true);
    expect(isSearchUrl("/saved-searches")).toBe(false);
    expect(isSearchUrl("/property/1")).toBe(false);
  });
});

describe("recordNavigation", () => {
  it("moves the current URL to previous and remembers searches", () => {
    let record = recordNavigation(null, "/search?transactionType=Sale");
    expect(record).toEqual({ previous: null, current: "/search?transactionType=Sale", lastSearch: "/search?transactionType=Sale" });

    record = recordNavigation(record, "/property/1");
    expect(record).toEqual({ previous: "/search?transactionType=Sale", current: "/property/1", lastSearch: "/search?transactionType=Sale" });

    record = recordNavigation(record, "/messages/new?listing=1");
    expect(record.previous).toBe("/property/1");
    // Only a search replaces the last search.
    expect(record.lastSearch).toBe("/search?transactionType=Sale");
  });

  it("keeps the previous URL when the same URL is recorded again", () => {
    const record = recordNavigation(recordNavigation(null, "/search"), "/property/1");
    expect(recordNavigation(record, "/property/1")).toEqual(record);
  });
});

describe("listingBackHref", () => {
  const listing = { transactionType: "Sale", propertyType: "Apartment" };

  it("is the last search, filters and page included", () => {
    expect(listingBackHref("/search?transactionType=Rent&page=3", listing)).toBe("/search?transactionType=Rent&page=3");
  });

  it("is a search for the same kind of listing when there's none", () => {
    expect(listingBackHref(null, listing)).toBe("/search?transactionType=Sale&propertyType=Apartment");
  });
});

describe("canGoBackTo", () => {
  it("only when the visitor came straight from that page", () => {
    expect(canGoBackTo("/property/1", "/property/1")).toBe(true);
    expect(canGoBackTo("/property/1", "/login")).toBe(false);
    expect(canGoBackTo("/property/1", null)).toBe(false);
  });
});
