// Product-analytics events (PostHog). Components call track(); without the visitor's consent to
// analytics cookies it does nothing at all — not even queueing, so an event from before "Accept"
// is never sent afterwards. With consent, events wait in a short queue until AnalyticsProvider has
// loaded PostHog (never, without a key). Properties are ids and categories only — never names,
// emails, phone numbers or message text.

import type { PostHog } from "posthog-js";
import { readConsent } from "@/lib/consent/consent";

export type AnalyticsEvent =
  | "listing_viewed"
  | "phone_revealed"
  | "messaging_app_opened"
  | "message_started"
  | "favorite_added"
  | "favorite_removed"
  | "saved_search_created"
  | "listing_created"
  | "search_performed"
  // Sent by the sign-up server actions (lib/analytics/serverEvents), not by track().
  | "account_created";

type Properties = Record<string, string | number | boolean | string[] | null>;

const MAX_QUEUED = 20;
let client: PostHog | null = null;
const queue: [AnalyticsEvent, Properties][] = [];

export function track(event: AnalyticsEvent, properties: Properties = {}): void {
  if (typeof document === "undefined" || readConsent()?.analytics !== true) return;
  if (client) client.capture(event, properties);
  else if (queue.length < MAX_QUEUED) queue.push([event, properties]);
}

// AnalyticsProvider, once PostHog is ready.
export function attachAnalyticsClient(posthog: PostHog): void {
  client = posthog;
  for (const [event, properties] of queue.splice(0)) posthog.capture(event, properties);
}

// The /search query string a search_performed event describes: which filters were used and the
// coarse ones' values (kind of offer and property, place ids). Never paging or the sort.
const VALUE_FILTERS = new Set(["transactionType", "propertyType", "raionId", "localitateId", "chisinauSectorId"]);
const NOT_FILTERS = new Set(["page", "sort", "view"]);

export function searchEventProperties(query: URLSearchParams, view: "list" | "map"): Properties {
  const filters = [...new Set([...query.keys()].filter((key) => !NOT_FILTERS.has(key)))].sort();
  const properties: Properties = { view, filters, filter_count: filters.length };
  for (const key of VALUE_FILTERS) {
    const values = query.getAll(key);
    if (values.length === 1) properties[key] = values[0];
    else if (values.length > 1) properties[key] = values;
  }
  return properties;
}
