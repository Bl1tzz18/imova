import { MAP_PATH, SEARCH_PATH, searchHref } from "@/lib/search/filters";

// Where "back" links lead. NavigationTracker (root layout) records, per browser tab, the previous
// in-app URL and the last search the visitor ran (/search or /map, with every filter, the sort and
// the page — it's all in the URL). BackLink uses them: the browser's own Back (scroll position and
// all) when the page it points to is exactly the one the visitor came from, a plain link otherwise.

export const PREVIOUS_URL_KEY = "imova:nav:previous";
export const CURRENT_URL_KEY = "imova:nav:current";
export const LAST_SEARCH_KEY = "imova:nav:lastSearch";

export function isSearchUrl(url: string): boolean {
  const path = url.split(/[?#]/, 1)[0];
  return path === SEARCH_PATH || path === MAP_PATH;
}

// What to store after navigating to `url`: it becomes the current URL, the one before it the
// previous (unless it's the same URL again — a re-render), and a search is remembered as the last one.
export type NavigationRecord = { previous: string | null; current: string; lastSearch: string | null };

export function recordNavigation(stored: NavigationRecord | null, url: string): NavigationRecord {
  const sameAsBefore = stored?.current === url;
  return {
    previous: sameAsBefore ? stored.previous : (stored?.current ?? null),
    current: url,
    lastSearch: isSearchUrl(url) ? url : (stored?.lastSearch ?? null),
  };
}

// A listing's "Înapoi la anunțuri": the visitor's last search, or — when they arrived from outside
// (Google, a shared link) — a search for the same kind of listing, never the home page.
export function listingBackHref(
  lastSearch: string | null,
  listing: { transactionType: string; propertyType: string },
): string {
  return lastSearch ?? searchHref({ transactionType: [listing.transactionType], propertyType: [listing.propertyType] });
}

// Whether a back link to `href` can just be the browser's Back: the visitor came straight from there.
export function canGoBackTo(href: string, previous: string | null): boolean {
  return previous !== null && previous === href;
}
