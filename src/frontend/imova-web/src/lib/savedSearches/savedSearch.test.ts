import { describe, expect, it } from "vitest";
import {
  alertChannels,
  alertChannelsKey,
  isAlertFrequency,
  openSavedSearchHref,
  queryToSave,
  toggleAlertChannel,
} from "./savedSearch";

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

describe("alert channels", () => {
  it("toggles one channel at a time, never to none", () => {
    expect(toggleAlertChannel({ email: true, telegram: false }, "telegram")).toEqual({ email: true, telegram: true });
    expect(toggleAlertChannel({ email: true, telegram: true }, "email")).toEqual({ email: false, telegram: true });
    expect(toggleAlertChannel({ email: true, telegram: false }, "email")).toBeNull();
  });

  it("names where the alerts go", () => {
    expect(alertChannelsKey({ email: true, telegram: false })).toBe("email");
    expect(alertChannelsKey({ email: false, telegram: true })).toBe("telegram");
    expect(alertChannelsKey({ email: true, telegram: true })).toBe("both");
    expect(alertChannels({ alertByEmail: false, alertByTelegram: true })).toEqual({ email: false, telegram: true });
  });
});
