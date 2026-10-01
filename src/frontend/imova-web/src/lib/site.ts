// The site's own public address — what canonical links, link previews (og:url) and structured data
// point at. SITE_URL is server-only and set per environment (compose: http://localhost:3000).
export function siteUrl(): string {
  return (process.env.SITE_URL ?? "http://localhost:3000").replace(/\/+$/, "");
}
