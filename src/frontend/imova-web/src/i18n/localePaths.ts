import { defaultLocale, isLocale, locales, type Locale } from "./config";

// Addresses per language, as pure functions (Vitest-covered) — what middleware.ts, the sitemap and
// page metadata agree on. Romanian is the bare path (/search), the others are prefixed (/ru/search).

// "/ru/search" → { locale: "ru", path: "/search" }; "/search" → { locale: null, path: "/search" }.
export function splitLocale(pathname: string): { locale: Locale | null; path: string } {
  const first = pathname.split("/")[1];
  if (!isLocale(first)) return { locale: null, path: pathname };
  const rest = pathname.slice(first.length + 1);
  return { locale: first, path: rest === "" ? "/" : rest };
}

// The address of a path (no language prefix, may carry a query) in a language.
export function localizedPath(locale: Locale, path: string): string {
  if (locale === defaultLocale) return path;
  return path === "/" ? `/${locale}` : path.startsWith("/?") ? `/${locale}${path.slice(1)}` : `/${locale}${path}`;
}

// The unprefixed page visit of someone whose language cookie says Russian or English (they picked
// it, or last opened a page in it) goes to that language's address — a link without a prefix (an
// old email, a bookmark, a link Google found) still opens in their language. Only real page visits
// (GET, Sec-Fetch-Dest document — or no such header, as older browsers send) and never search
// engines, which carry no cookie. Background requests (prefetches, images, the router's data
// fetches) are left alone: a redirect there would only cost a round trip.
export function localeRedirect(request: {
  method: string;
  pathname: string;
  search: string;
  secFetchDest: string | null;
  cookieLocale: string | undefined;
}): string | null {
  if (request.method !== "GET") return null;
  if (request.secFetchDest != null && request.secFetchDest !== "document") return null;
  if (!isLocale(request.cookieLocale) || request.cookieLocale === defaultLocale) return null;
  if (splitLocale(request.pathname).locale) return null;
  return localizedPath(request.cookieLocale, request.pathname) + request.search;
}

// hreflang alternates for a page — the same path in every language, plus x-default (Romanian) —
// as an HTTP Link header, which Google reads like the <link rel="alternate"> tags.
export function alternateLinksHeader(siteUrl: string, path: string, search: string): string {
  const url = (locale: Locale) => `${siteUrl}${localizedPath(locale, path)}${search}`;
  return [
    ...locales.map((locale) => `<${url(locale)}>; rel="alternate"; hreflang="${locale}"`),
    `<${url(defaultLocale)}>; rel="alternate"; hreflang="x-default"`,
  ].join(", ");
}

// Per-page metadata: the page's own address as canonical, the others as alternates. `path` has no
// prefix and may carry a query.
export function localeAlternates(locale: Locale, path: string): {
  canonical: string;
  languages: Record<string, string>;
} {
  return {
    canonical: localizedPath(locale, path),
    languages: {
      ...Object.fromEntries(locales.map((l) => [l, localizedPath(l, path)])),
      "x-default": localizedPath(defaultLocale, path),
    },
  };
}
