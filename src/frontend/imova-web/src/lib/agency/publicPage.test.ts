import { describe, expect, it } from "vitest";
import type { Agency } from "@/types/agency";
import {
  agencyDirectoryApiQuery,
  agencyDirectoryHref,
  agencyJsonLd,
  agencyListingsHref,
  agencyListingsSearchQuery,
  agencyMetaDescription,
  agencyPath,
  bioIsLong,
  hasListingFilters,
  parseAgencyDirectoryParams,
  parseAgencyListingsParams,
  websiteLabel,
} from "./publicPage";

const RAION = "0b8a5a2e-6f2c-4f7e-9a51-2a3c1d4e5f60";
const AGENCY_ID = "7c497849-f52b-4b0c-95fc-244cc3b7b8cb";

const agency: Agency = {
  id: AGENCY_ID,
  slug: "casa-ta-imobiliare",
  name: "Casa Ta Imobiliare",
  logoUrl: "https://blob.example/logo_512.jpg",
  logoThumbnailUrl: "https://blob.example/logo_128.jpg",
  bio: "Agenție din Chișinău.\nVânzări și chirii.",
  phone: null,
  phonePrefix: "+373 22",
  phoneHiddenDigits: 4,
  email: "office@casata.md",
  website: "https://casata.md",
  address: "str. Ismail 44",
  raionId: RAION,
  raionName: "Chișinău",
  isVerified: true,
  verifiedAt: null,
  status: "Active",
  createdAt: "2026-10-01T09:00:00Z",
  memberCount: 2,
  activeListingCount: 5,
  myRole: null,
};

describe("agency listings state", () => {
  it("reads the filters it knows and drops the rest", () => {
    expect(parseAgencyListingsParams({ transactionType: "Rent", propertyType: "House", sort: "PriceAsc", page: "3" })).toEqual({
      transactionType: "Rent",
      propertyType: "House",
      sort: "PriceAsc",
      page: 3,
    });
    expect(parseAgencyListingsParams({ transactionType: "Swap", propertyType: "Castle", sort: "AreaAsc", page: "-2" })).toEqual({
      transactionType: undefined,
      propertyType: undefined,
      sort: "Newest",
      page: 1,
    });
  });

  it("builds the page's URL without the defaults", () => {
    expect(agencyListingsHref("casa-ta", { sort: "Newest", page: 1 })).toBe("/agencies/casa-ta");
    expect(agencyListingsHref("casa-ta", { transactionType: "Sale", sort: "PriceDesc", page: 2 })).toBe(
      "/agencies/casa-ta?transactionType=Sale&sort=PriceDesc&page=2",
    );
  });

  it("asks the search for the agency's listings with every filter", () => {
    const query = agencyListingsSearchQuery(AGENCY_ID, { transactionType: "Sale", propertyType: "Apartment", sort: "Newest", page: 2 });
    expect(new URLSearchParams(query).get("agencyId")).toBe(AGENCY_ID);
    expect(query).toContain("transactionType=Sale");
    expect(query).toContain("propertyType=Apartment");
    expect(query).toContain("page=2");
    expect(query).toContain("pageSize=24");
  });

  it("counts only real filters as filters", () => {
    expect(hasListingFilters({ sort: "PriceAsc", page: 4 })).toBe(false);
    expect(hasListingFilters({ propertyType: "Land", sort: "Newest", page: 1 })).toBe(true);
  });

  it("escapes the slug in the path", () => {
    expect(agencyPath("a b")).toBe("/agencies/a%20b");
  });
});

describe("agency directory state", () => {
  it("reads the search, a valid city id and the verified switch", () => {
    expect(parseAgencyDirectoryParams({ q: "  casa ", raionId: RAION, verified: "1", page: "2" })).toEqual({
      q: "casa",
      raionId: RAION,
      verified: true,
      page: 2,
    });
    expect(parseAgencyDirectoryParams({ raionId: "chisinau", verified: "yes" })).toEqual({
      q: undefined,
      raionId: undefined,
      verified: false,
      page: 1,
    });
  });

  it("cuts a very long search", () => {
    expect(parseAgencyDirectoryParams({ q: "x".repeat(300) }).q).toHaveLength(100);
  });

  it("builds the page URL and the API query", () => {
    expect(agencyDirectoryHref({ verified: false, page: 1 })).toBe("/agencies");
    expect(agencyDirectoryHref({ q: "casa ta", verified: true, page: 3 })).toBe("/agencies?q=casa+ta&verified=1&page=3");
    expect(agencyDirectoryApiQuery({ q: "casa", raionId: RAION, verified: true, page: 1 })).toBe(
      `q=casa&raionId=${RAION}&verified=true&page=1&pageSize=24`,
    );
  });
});

describe("how the agency looks", () => {
  it("folds a long bio, or one with many lines", () => {
    expect(bioIsLong("Scurt.")).toBe(false);
    expect(bioIsLong("a".repeat(281))).toBe(true);
    expect(bioIsLong("1\n2\n3\n4\n5")).toBe(true);
  });

  it("describes the agency with its bio's opening, else the fallback", () => {
    expect(agencyMetaDescription("Agenție\n\ndin   Chișinău.", "Fallback")).toBe("Agenție din Chișinău.");
    expect(agencyMetaDescription("cuvânt ".repeat(60), "Fallback").length).toBeLessThanOrEqual(155);
    expect(agencyMetaDescription(null, "Fallback")).toBe("Fallback");
    expect(agencyMetaDescription("   ", "Fallback")).toBe("Fallback");
  });

  it("shows a website without its scheme", () => {
    expect(websiteLabel("https://casata.md/")).toBe("casata.md");
    expect(websiteLabel("http://www.casata.md/despre")).toBe("www.casata.md/despre");
  });

  it("is a RealEstateAgent in structured data, without a phone number", () => {
    const data = agencyJsonLd(agency, "https://imova.md/agencies/casa-ta-imobiliare");
    expect(data).toMatchObject({
      "@type": "RealEstateAgent",
      name: "Casa Ta Imobiliare",
      url: "https://imova.md/agencies/casa-ta-imobiliare",
      logo: agency.logoUrl,
      email: "office@casata.md",
      sameAs: ["https://casata.md"],
      address: { "@type": "PostalAddress", addressCountry: "MD", addressRegion: "Chișinău", streetAddress: "str. Ismail 44" },
    });
    expect(data).not.toHaveProperty("telephone");
  });

  it("leaves out what the agency didn't fill in", () => {
    const data = agencyJsonLd({ ...agency, logoUrl: null, bio: null, website: null, address: null, raionName: null }, "u");
    expect(data).not.toHaveProperty("logo");
    expect(data).not.toHaveProperty("description");
    expect(data).not.toHaveProperty("sameAs");
    expect(data).not.toHaveProperty("address");
  });
});
