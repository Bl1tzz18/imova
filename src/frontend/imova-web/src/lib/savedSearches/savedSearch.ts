import { toQueryString, type SearchState } from "@/lib/search/filters";

// Saved searches (backend: SavedSearch / SavedSearchEndpoints). A saved search is the /search
// page's own query string, so opening one is just /search?{queryString}.

export const ALERT_FREQUENCIES = ["Off", "Daily", "Instant"] as const;
export type AlertFrequency = (typeof ALERT_FREQUENCIES)[number];

export type SavedSearch = {
  id: string;
  name: string;
  queryString: string;
  alertFrequency: AlertFrequency;
  newListingsCount: number;
  createdAt: string;
  // Where the alerts go — at least one is true.
  alertByEmail: boolean;
  alertByTelegram: boolean;
};

export type AlertChannels = { email: boolean; telegram: boolean };

export function alertChannels(search: Pick<SavedSearch, "alertByEmail" | "alertByTelegram">): AlertChannels {
  return { email: search.alertByEmail, telegram: search.alertByTelegram };
}

// Turning one channel on or off; null when that would leave none (no alerts at all is "Off").
export function toggleAlertChannel(channels: AlertChannels, channel: keyof AlertChannels): AlertChannels | null {
  const next = { ...channels, [channel]: !channels[channel] };
  return next.email || next.telegram ? next : null;
}

// Which "where" text describes the channels (SavedSearches.where.*).
export function alertChannelsKey(channels: AlertChannels): "email" | "telegram" | "both" {
  if (channels.email && channels.telegram) return "both";
  return channels.telegram ? "telegram" : "email";
}

export const SAVED_SEARCHES_PATH = "/saved-searches";

// What gets saved for the search currently shown: every filter and the sort, not which page of
// results the user happens to be on (the backend drops it too).
export function queryToSave(state: SearchState): string {
  const rest = Object.fromEntries(Object.entries(state).filter(([key]) => key !== "page"));
  return toQueryString(rest);
}

// Opens a saved search: resets its "new since your last visit" count, then goes to the results.
export function openSavedSearchHref(id: string): string {
  return `${SAVED_SEARCHES_PATH}/${id}/open`;
}

export function isAlertFrequency(value: unknown): value is AlertFrequency {
  return typeof value === "string" && (ALERT_FREQUENCIES as readonly string[]).includes(value);
}
