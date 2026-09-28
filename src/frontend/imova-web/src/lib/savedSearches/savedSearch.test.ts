import { describe, expect, it } from "vitest";
import { isAlertFrequency, openSavedSearchHref, queryToSave } from "./savedSearch";

describe("queryToSave", () => {
  it("keeps every filter and the sort, but not the page", () => {
    expect(queryToSave({ transactionType: ["Rent"], propertyType: ["Apartment"], sort: ["PriceAsc"], page: ["3"] })).toBe(
      "transactionType=Rent&propertyType=Apartment&sort=PriceAsc",
    );
  });

  it("is empty for a search without filters", () => {
    expect(queryToSave({})).toBe("");
  });
});

describe("openSavedSearchHref", () => {
  it("goes through the open route that marks the search as viewed", () => {
    expect(openSavedSearchHref("abc")).toBe("/saved-searches/abc/open");
  });
});

describe("isAlertFrequency", () => {
  it("accepts only the three frequencies", () => {
    expect(isAlertFrequency("Daily")).toBe(true);
    expect(isAlertFrequency("Weekly")).toBe(false);
    expect(isAlertFrequency(undefined)).toBe(false);
  });
});
