import { siteUrl } from "@/lib/site";
import { sitemapFiles, sitemapIndexXml, type SitemapListingPage } from "@/lib/seo/sitemap";

// /sitemap.xml — the index: the site's pages, then one file per 10 000 listings (see
// lib/seo/sitemap.ts). Built on every request from the API (listings come and go), never at build
// time — the API isn't reachable while the Docker image builds.
export const dynamic = "force-dynamic";

export async function GET() {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/sitemap/listings?page=1`, { cache: "no-store" });
  if (!res.ok) {
    return new Response(null, { status: 503, headers: { "Retry-After": "300" } });
  }
  const { total, pageSize } = (await res.json()) as SitemapListingPage;

  return new Response(sitemapIndexXml(siteUrl(), sitemapFiles(total, pageSize)), {
    headers: { "Content-Type": "application/xml; charset=utf-8", "Cache-Control": "public, max-age=3600" },
  });
}
