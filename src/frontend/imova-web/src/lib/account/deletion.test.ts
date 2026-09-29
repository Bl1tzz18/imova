import { describe, expect, it } from "vitest";
import { canConfirmDeletion, deletionItems, type AccountDataSummary } from "@/lib/account/deletion";

const empty: AccountDataSummary = {
  listings: 0,
  activeListings: 0,
  favorites: 0,
  savedSearches: 0,
  conversations: 0,
  hasAgency: false,
  hasPassword: true,
};

describe("deletionItems", () => {
  it("lists nothing for an account that holds nothing", () => {
    expect(deletionItems(empty)).toEqual([]);
  });

  it("lists only what the account has, listings first", () => {
    expect(deletionItems({ ...empty, listings: 3, activeListings: 1, savedSearches: 2, hasAgency: true })).toEqual([
      { key: "listings", count: 3 },
      { key: "agency", count: 1 },
      { key: "savedSearches", count: 2 },
    ]);
  });
});

describe("canConfirmDeletion", () => {
  it("needs the acknowledgement", () => {
    expect(canConfirmDeletion({ hasPassword: true, password: "Secret1!", acknowledged: false })).toBe(false);
  });

  it("needs the password when the account has one", () => {
    expect(canConfirmDeletion({ hasPassword: true, password: "", acknowledged: true })).toBe(false);
    expect(canConfirmDeletion({ hasPassword: true, password: "Secret1!", acknowledged: true })).toBe(true);
  });

  it("needs no password for an account without one (it confirms by email)", () => {
    expect(canConfirmDeletion({ hasPassword: false, password: "", acknowledged: true })).toBe(true);
  });
});
