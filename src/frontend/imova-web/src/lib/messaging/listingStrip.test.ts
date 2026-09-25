import { describe, expect, it } from "vitest";
import { canOpenListing } from "@/lib/messaging/listingStrip";
import type { ConversationListingDetails } from "@/types/messaging";

const listing = (isActive: boolean): ConversationListingDetails => ({
  id: "l1",
  title: "Apartament 2 camere",
  photoUrl: null,
  propertyType: "Apartment",
  transactionType: "Rent",
  totalAreaM2: 54,
  rooms: 2,
  priceAmount: 550,
  priceCurrency: "EUR",
  isActive,
});

describe("canOpenListing", () => {
  it("links a live listing for both participants", () => {
    expect(canOpenListing(listing(true), true)).toBe(true);
    expect(canOpenListing(listing(true), false)).toBe(true);
  });

  it("links an inactive listing only for its publisher (the visitor would get a 404)", () => {
    expect(canOpenListing(listing(false), false)).toBe(true);
    expect(canOpenListing(listing(false), true)).toBe(false);
  });

  it("never links a deleted listing", () => {
    expect(canOpenListing(null, false)).toBe(false);
  });
});
