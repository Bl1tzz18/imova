// Anonymous page-view statistics for every visitor, whatever they chose in the cookie banner: built
// on the server from what a page request carries anyway, so nothing is read from or stored on the
// visitor's device, and with nothing that tells two visits apart — no IP, no full User-Agent, no
// cookie, no id. Aggregate counts only (no unique visitors, no paths through the site); product
// analytics with consent is PostHog's job. Sent to Application Insights as a "PageView" custom
// event (lib/monitoring/pageViewEvents.ts) and shown in the Azure workbook (infra, monitoring.tf).
//
// Only full page loads count — arriving on the site, opening a link, a reload — not clicks inside
// the site: Next takes the headers that tell such a click from a link prefetch (RSC,
// Next-Router-Prefetch, _rsc) off the request before middleware sees it, and counting both would
// inflate every number. Entries are what traffic sources are about anyway.

export type PageView = {
  // The kind of page, never an id: "/property/[slug]", "/search", "other".
  page: string;
  locale: string;
  device: "mobile" | "tablet" | "desktop";
  // Where the visit came from: a referring site's domain, "direct", or "internal" (a full load
  // from an IMOVA page, e.g. a reload or a link opened in a new tab).
  source: string;
  // Campaign tags from the address (utm_source / utm_medium / utm_campaign), lower-cased and cut short.
  utmSource: string | null;
  utmMedium: string | null;
  utmCampaign: string | null;
};

export type PageRequest = {
  method: string;
  // Without the language prefix (splitLocale), and the language on its own.
  path: string;
  locale: string;
  search: string;
  siteHost: string;
  headers: { get(name: string): string | null };
};

// Pages of the site (first path segment). Anything else — probes like /wp-admin, mistyped
// addresses — is "other", so random URLs can't flood the statistics.
const SECTIONS = new Set([
  "account",
  "account-deleted",
  "admin",
  "agencies",
  "complete-profile",
  "confirm-email",
  "dashboard",
  "delete-account",
  "favorites",
  "forgot-password",
  "invitations",
  "login",
  "map",
  "messages",
  "my-listings",
  "privacy",
  "properties",
  "property",
  "register",
  "reset-password",
  "saved-listings",
  "saved-searches",
  "search",
  "terms",
]);

// Paths that aren't pages a person reads: file downloads and redirects.
const NOT_PAGES = [/^\/message-attachments\//, /^\/account\/data-export$/, /^\/saved-searches\/[^/]+\/open$/];

// Segments that name one record, by the page's route.
const DYNAMIC = [
  [/^\/property\/[^/]+$/, "/property/[slug]"],
  [/^\/agencies\/[^/]+$/, "/agencies/[slug]"],
  [/^\/invitations\/[^/]+$/, "/invitations/[token]"],
  [/^\/messages\/(?!new$)[^/]+$/, "/messages/[id]"],
  [/^\/my-listings\/[^/]+\/edit$/, "/my-listings/[id]/edit"],
  [/^\/my-listings\/[^/]+$/, "/my-listings/[id]"],
  [/^\/account\/agencies\/[^/]+(\/.*)?$/, "/account/agencies/[id]"],
  [/^\/admin(\/.*)?$/, "/admin"],
] as const;

const BOT = /bot|crawl|spider|slurp|facebookexternalhit|embedly|preview|monitor|uptime|headless|lighthouse|pingdom|curl|wget|python|java\/|go-http/i;

export function pageTemplate(path: string): string | null {
  const clean = path.length > 1 ? path.replace(/\/+$/, "") : path;
  if (NOT_PAGES.some((p) => p.test(clean))) return null;
  if (clean === "/") return "/";
  if (!SECTIONS.has(clean.split("/")[1])) return "other";
  for (const [pattern, template] of DYNAMIC) if (pattern.test(clean)) return template;
  // A known section's own static pages (/search, /saved-searches/unsubscribe, /properties/new);
  // anything with an id-like segment that the list above doesn't know is cut back to its section.
  return clean.split("/").some((s) => /\d/.test(s) || s.length > 40) ? `/${clean.split("/")[1]}/[id]` : clean;
}

export function deviceOf(userAgent: string): PageView["device"] {
  if (/iPad|Tablet|PlayBook|Silk|Android(?!.*Mobile)/i.test(userAgent)) return "tablet";
  if (/Mobi|iPhone|iPod|Android|Windows Phone/i.test(userAgent)) return "mobile";
  return "desktop";
}

export function sourceOf(referrer: string | null, siteHost: string): string {
  if (!referrer) return "direct";
  let host: string;
  try {
    host = new URL(referrer).hostname.toLowerCase();
  } catch {
    return "direct";
  }
  const bare = (h: string) => h.replace(/^www\./, "");
  if (bare(host) === bare(siteHost.toLowerCase())) return "internal";
  // Android apps send "android-app://com.google.android.gm" — the hostname is the app.
  return bare(host).slice(0, 100) || "direct";
}

function tag(value: string | null): string | null {
  const v = value?.trim().toLowerCase().slice(0, 50);
  return v ? v : null;
}

// Null when the request isn't a full page load by a person: not a GET, not the browser loading a
// document (Next's in-site fetches and prefetches, images, scripts), a browser prefetch, a bot, or
// not a page at all. Browsers without Sec-Fetch headers (old Safari) count when they ask for HTML.
export function pageViewFrom(request: PageRequest): PageView | null {
  const h = request.headers;
  if (request.method !== "GET") return null;
  if (h.get("purpose") === "prefetch" || h.get("sec-purpose")?.includes("prefetch")) return null;
  const dest = h.get("sec-fetch-dest");
  const isDocument = dest ? dest === "document" : (h.get("accept") ?? "").includes("text/html");
  if (!isDocument) return null;
  const userAgent = h.get("user-agent") ?? "";
  if (!userAgent || BOT.test(userAgent)) return null;
  const page = pageTemplate(request.path);
  if (!page) return null;

  const params = new URLSearchParams(request.search);
  return {
    page,
    locale: request.locale,
    device: deviceOf(userAgent),
    source: sourceOf(h.get("referer"), request.siteHost),
    utmSource: tag(params.get("utm_source")),
    utmMedium: tag(params.get("utm_medium")),
    utmCampaign: tag(params.get("utm_campaign")),
  };
}
