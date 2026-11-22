import { describe, expect, it } from "vitest";
import { listingPath, listingSlug, parseListingSlug, slugify } from "./listingUrl";

describe("slugify", () => {
  it("writes Romanian titles in plain Latin letters", () => {
    expect(slugify("Apartament cu 2 camere, bloc nou, Botanica")).toBe("apartament-cu-2-camere-bloc-nou-botanica");
    expect(slugify("Casă în Ștefan Vodă, țară liniștită")).toBe("casa-in-stefan-voda-tara-linistita");
    expect(slugify("Casă cu ş şi ţ vechi")).toBe("casa-cu-s-si-t-vechi");
  });

  it("transliterates Russian titles", () => {
    expect(slugify("Квартира 2 комнаты, Ботаника")).toBe("kvartira-2-komnaty-botanika");
    expect(slugify("Дом с садом, Яловены")).toBe("dom-s-sadom-ialoveny");
  });

  it("drops punctuation and symbols, keeping no stray dashes", () => {
    expect(slugify("  !!! 82 000 € — urgent!!  ")).toBe("82-000-urgent");
    expect(slugify("€€€")).toBe("");
  });

  it("cuts a long title on a word boundary", () => {
    const slug = slugify("Apartament spațios cu trei camere și două băi, renovat complet, mobilat, aproape de parc");
    expect(slug.length).toBeLessThanOrEqual(60);
    expect(slug).toBe("apartament-spatios-cu-trei-camere-si-doua-bai-renovat");
  });
});

describe("listingSlug / listingPath", () => {
  it("puts the number first", () => {
    expect(listingSlug(100015, "Apartament cu 2 camere")).toBe("100015-apartament-cu-2-camere");
    expect(listingSlug(100015, "€€€")).toBe("100015");
    expect(listingPath({ id: "x", number: 100015, title: "Garaj, Ciocana" })).toBe("/property/100015-garaj-ciocana");
  });

  it("falls back to the id when the number isn't known", () => {
    expect(listingPath({ id: "3f2c…" })).toBe("/property/3f2c…");
  });
});

describe("parseListingSlug", () => {
  it("reads the number, whatever words follow", () => {
    expect(parseListingSlug("100015-apartament-cu-2-camere")).toEqual({ kind: "number", number: 100015 });
    expect(parseListingSlug("100015")).toEqual({ kind: "number", number: 100015 });
    expect(parseListingSlug("100015-%D0%BA%D0%B2")).toEqual({ kind: "number", number: 100015 });
  });

  it("reads a legacy id", () => {
    expect(parseListingSlug("0E2A8C34-1B2C-4D5E-8F90-123456789ABC")).toEqual({
      kind: "id",
      id: "0e2a8c34-1b2c-4d5e-8f90-123456789abc",
    });
  });

  it("refuses anything else", () => {
    expect(parseListingSlug("apartament-100015")).toBeNull();
    expect(parseListingSlug("100015abc")).toBeNull();
    expect(parseListingSlug("")).toBeNull();
  });
});
