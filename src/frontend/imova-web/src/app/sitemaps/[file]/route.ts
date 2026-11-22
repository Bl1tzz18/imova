import { siteUrl } from "@/lib/site";
import {
  agencyEntries,
  listingEntries,
  listingFilePage,
  PAGES_FILE,
  sitePages,
  urlsetXml,
  type SitemapAgency,
  type SitemapEntry,
  type SitemapListingPage,
} from "@/lib/seo/sitemap";

// One file of the sitemap index (/sitemap.xml): pages.xml (the site's own pages + agency pages) or
// listings-N.xml (the N-th 10 000 public listings). Every page in all three languages.
export const dynamic = "force-dynamic";

export async function GET(_request: Request, { params }: { params: Promise<{ file: string }> }) {
  const { file } = await params;
  const entries = await entriesFor(file);
  if (entries === "notFound") return new Response(null, { status: 404 });
  if (entries === "unavailable") return new Response(null, { status: 503, headers: { "Retry-After": "300" } });

  return new Response(urlsetXml(siteUrl(), entries), {
    headers: { "Content-Type": "application/xml; charset=utf-8", "Cache-Control": "public, max-age=3600" },
  });
}

async function entriesFor(file: string): Promise<SitemapEntry[] | "notFound" | "unavailable"> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  if (file === PAGES_FILE) {
    const res = await fetch(`${apiUrl}/api/v1/sitemap/agencies`, { cache: "no-store" });
    if (!res.ok) return "unavailable";
    return [...sitePages(), ...agencyEntries((await res.json()) as SitemapAgency[])];
  }

  const page = listingFilePage(file);
  if (page === null) return "notFound";
  const res = await fetch(`${apiUrl}/api/v1/sitemap/listings?page=${page}`, { cache: "no-store" });
  if (!res.ok) return "unavailable";
  const { items } = (await res.json()) as SitemapListingPage;
  return items.length > 0 ? listingEntries(items) : "notFound";
}
