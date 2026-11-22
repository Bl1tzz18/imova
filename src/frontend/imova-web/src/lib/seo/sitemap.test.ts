import { describe, expect, it } from "vitest";
import {
  agencyEntries,
  escapeXml,
  listingEntries,
  listingFilePage,
  sitemapFiles,
  sitemapIndexXml,
  sitePages,
  urlsetXml,
} from "./sitemap";

describe("sitePages", () => {
  it("has home, the agencies and search per transaction and property type", () => {
    const paths = sitePages().map((p) => p.path);
    expect(paths).toContain("/");
    expect(paths).toContain("/agencies");
    expect(paths).toContain("/search?transactionType=Sale");
    expect(paths).toContain("/search?transactionType=Rent&propertyType=Apartment");
    expect(paths).toHaveLength(2 + 2 * 7);
  });
});

describe("entries", () => {
  it("points at the listing's readable address and the agency's page", () => {
    expect(listingEntries([{ number: 100015, title: "Garaj, Ciocana", lastModified: "2026-11-20T10:00:00Z" }])).toEqual([
      { path: "/property/100015-garaj-ciocana", lastModified: "2026-11-20T10:00:00Z" },
    ]);
    expect(agencyEntries([{ slug: "casa-ta", lastModified: "2026-11-01T00:00:00Z" }])).toEqual([
      { path: "/agencies/casa-ta", lastModified: "2026-11-01T00:00:00Z" },
    ]);
  });
});

describe("files", () => {
  it("lists the pages file, then one file per page of listings", () => {
    expect(sitemapFiles(0, 10_000)).toEqual(["pages.xml"]);
    expect(sitemapFiles(10_000, 10_000)).toEqual(["pages.xml", "listings-1.xml"]);
    expect(sitemapFiles(10_001, 10_000)).toEqual(["pages.xml", "listings-1.xml", "listings-2.xml"]);
  });

  it("reads a listings file's page number, and nothing else", () => {
    expect(listingFilePage("listings-3.xml")).toBe(3);
    expect(listingFilePage("listings-0.xml")).toBeNull();
    expect(listingFilePage("listings-3.xml.gz")).toBeNull();
    expect(listingFilePage("pages.xml")).toBeNull();
  });
});

describe("xml", () => {
  it("writes the index", () => {
    expect(sitemapIndexXml("https://imova.md", ["pages.xml"])).toContain(
      "<sitemap><loc>https://imova.md/sitemaps/pages.xml</loc></sitemap>",
    );
  });

  it("lists each page once per language, each naming all three", () => {
    const xml = urlsetXml("https://imova.md", [{ path: "/search?transactionType=Sale&propertyType=House", lastModified: "2026-11-20T10:00:00Z" }]);

    expect(xml.match(/<url>/g)).toHaveLength(3);
    expect(xml).toContain("<loc>https://imova.md/search?transactionType=Sale&amp;propertyType=House</loc>");
    expect(xml).toContain("<loc>https://imova.md/ru/search?transactionType=Sale&amp;propertyType=House</loc>");
    expect(xml).toContain("<loc>https://imova.md/en/search?transactionType=Sale&amp;propertyType=House</loc>");
    expect(xml).toContain('<xhtml:link rel="alternate" hreflang="x-default" href="https://imova.md/search?transactionType=Sale&amp;propertyType=House"/>');
    expect(xml).toContain("<lastmod>2026-11-20T10:00:00.000Z</lastmod>");
    expect(xml.match(/hreflang="ru"/g)).toHaveLength(3);
  });

  it("escapes what XML can't hold", () => {
    expect(escapeXml(`a&b<c>"d'`)).toBe("a&amp;b&lt;c&gt;&quot;d&apos;");
  });
});
