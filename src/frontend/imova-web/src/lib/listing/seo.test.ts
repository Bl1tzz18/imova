import { describe, expect, it } from "vitest";
import type { Listing } from "@/types/listing";
import { coverPhotoUrl, jsonLdScript, listingJsonLd, listingMetaDescription, truncateText } from "./seo";

const listing = {
  id: "l1",
  title: "Apartament cu 2 camere, bloc nou, Botanica",
  description: "Apartament luminos cu 2 camere într-un bloc nou.\n\nLângă parc.",
  transactionType: "Sale",
  publishedAt: "2026-09-30T10:00:00Z",
  price: { amount: 82_000, currency: "EUR", priceEur: 82_000, isNegotiable: true },
  photos: [
    { url: "https://blob/1.jpg", isPrimary: false },
    { url: "https://blob/2.jpg", isPrimary: true },
  ],
  property: {
    propertyType: "Apartment",
    totalAreaM2: 62,
    typeSpecificAttributes: { rooms: 2 },
    location: {
      raionName: "Chișinău",
      localitateName: null,
      chisinauSectorName: "Botanica",
      street: "Strada Dacia",
      buildingNumber: "47",
      latitude: 47.0,
      longitude: 28.8,
    },
  },
} as unknown as Listing;

describe("truncateText", () => {
  it("keeps short text, collapsing whitespace", () => {
    expect(truncateText("a\n\n b", 10)).toBe("a b");
  });

  it("cuts on a word with an ellipsis", () => {
    expect(truncateText("Apartament luminos cu două camere", 20)).toBe("Apartament luminos…");
  });
});

describe("listingMetaDescription", () => {
  it("puts the facts first, then the owner's words, within 160 characters", () => {
    const text = listingMetaDescription(["82 000 €", "Apartament", "62 m²", null, "Chișinău, Botanica"], listing.description);
    expect(text.startsWith("82 000 € · Apartament · 62 m² · Chișinău, Botanica. Apartament luminos")).toBe(true);
    expect(text.length).toBeLessThanOrEqual(160);
  });
});

describe("coverPhotoUrl", () => {
  it("is the primary photo, else the first, else none", () => {
    expect(coverPhotoUrl(listing)).toBe("https://blob/2.jpg");
    expect(coverPhotoUrl({ photos: [] })).toBeNull();
  });
});

describe("listingJsonLd", () => {
  const data = listingJsonLd(listing, "https://imova.md/property/l1");

  it("is a RealEstateListing with its offer", () => {
    expect(data["@type"]).toBe("RealEstateListing");
    expect(data.offers).toMatchObject({ price: 82_000, priceCurrency: "EUR", businessFunction: "http://purl.org/goodrelations/v1#Sell" });
  });

  it("describes the place: area, rooms, address, coordinates", () => {
    expect(data.about).toMatchObject({
      numberOfRooms: 2,
      floorSize: { value: 62, unitCode: "MTK" },
      address: { addressRegion: "Chișinău", addressLocality: "Botanica", streetAddress: "Strada Dacia 47" },
      geo: { latitude: 47.0, longitude: 28.8 },
    });
  });

  it("can't break out of its script tag", () => {
    expect(jsonLdScript({ name: "</script><script>alert(1)</script>" })).not.toContain("</script>");
  });
});
