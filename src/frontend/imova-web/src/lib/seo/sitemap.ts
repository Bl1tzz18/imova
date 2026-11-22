import { locales, defaultLocale, type Locale } from "@/i18n/config";
import { localizedPath } from "@/i18n/localePaths";
import { agencyPath } from "@/lib/agency/publicPage";
import { listingPath } from "@/lib/listing/listingUrl";
import { PROPERTY_TYPES } from "@/lib/property/attributeSchema";
import { searchHref, TRANSACTION_TYPES } from "@/lib/search/filters";

// sitemap.xml, as pure functions (Vitest-covered). /sitemap.xml is an index of files: one with the
// site's own pages and the agency pages (/sitemaps/pages.xml), then the listings, 10 000 per file
// (/sitemaps/listings-1.xml, …). Every page appears once per language, each entry naming all three
// (hreflang), as search engines expect.

export type SitemapEntry = { path: string; lastModified?: string };

export type SitemapListing = { number: number; title: string; lastModified: string };
export type SitemapListingPage = { total: number; pageSize: number; items: SitemapListing[] };
export type SitemapAgency = { slug: string; lastModified: string };

export const PAGES_FILE = "pages.xml";

// The site's own entry points: home, the agency directory, the legal pages, and search for each
// transaction — on its own and per property type (what people search for: "apartamente de vânzare").
export function sitePages(): SitemapEntry[] {
  const searches = TRANSACTION_TYPES.flatMap((transactionType) => [
    searchHref({ transactionType: [transactionType] }),
    ...PROPERTY_TYPES.map((propertyType) => searchHref({ transactionType: [transactionType], propertyType: [propertyType] })),
  ]);
  return ["/", "/agencies", "/terms", "/privacy", ...searches].map((path) => ({ path }));
}

export function agencyEntries(agencies: SitemapAgency[]): SitemapEntry[] {
  return agencies.map((a) => ({ path: agencyPath(a.slug), lastModified: a.lastModified }));
}

export function listingEntries(listings: SitemapListing[]): SitemapEntry[] {
  return listings.map((l) => ({ path: listingPath({ id: "", number: l.number, title: l.title }), lastModified: l.lastModified }));
}

// "listings-3.xml" → 3; anything else → null.
export function listingFilePage(file: string): number | null {
  const match = /^listings-([1-9]\d{0,5})\.xml$/.exec(file);
  return match ? Number(match[1]) : null;
}

export function sitemapFiles(listingTotal: number, pageSize: number): string[] {
  const listingFiles = Math.ceil(listingTotal / pageSize);
  return [PAGES_FILE, ...Array.from({ length: listingFiles }, (_, i) => `listings-${i + 1}.xml`)];
}

export function sitemapIndexXml(siteUrl: string, files: string[]): string {
  const items = files.map((f) => `  <sitemap><loc>${escapeXml(`${siteUrl}/sitemaps/${f}`)}</loc></sitemap>`);
  return `<?xml version="1.0" encoding="UTF-8"?>
<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${items.join("\n")}
</sitemapindex>
`;
}

export function urlsetXml(siteUrl: string, entries: SitemapEntry[]): string {
  const url = (locale: Locale, path: string) => escapeXml(`${siteUrl}${localizedPath(locale, path)}`);
  const urls = entries.flatMap((entry) => {
    const alternates = [
      ...locales.map((l) => `    <xhtml:link rel="alternate" hreflang="${l}" href="${url(l, entry.path)}"/>`),
      `    <xhtml:link rel="alternate" hreflang="x-default" href="${url(defaultLocale, entry.path)}"/>`,
    ].join("\n");
    const lastmod = entry.lastModified ? `\n    <lastmod>${new Date(entry.lastModified).toISOString()}</lastmod>` : "";
    return locales.map((locale) => `  <url>\n    <loc>${url(locale, entry.path)}</loc>${lastmod}\n${alternates}\n  </url>`);
  });
  return `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9" xmlns:xhtml="http://www.w3.org/1999/xhtml">
${urls.join("\n")}
</urlset>
`;
}

export function escapeXml(text: string): string {
  return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&apos;");
}
