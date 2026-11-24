// The body of a server-side PostHog capture (lib/analytics/serverEvents). Pure, so Vitest covers it.

import { consentFromCookieHeader } from "@/lib/consent/consent";

export type ServerEventProperties = Record<string, string | number | boolean>;

export type ServerCapturePayload = {
  api_key: string;
  event: string;
  distinct_id: string;
  properties: ServerEventProperties;
};

// Null unless the visitor accepted analytics cookies and PostHog has already given this browser an
// id (its "ph_<key>_posthog" cookie, set once PostHog loads after "Accept") — the event then belongs
// to the same visitor as their page views, and to their account once the browser identifies them.
export function serverCapturePayload(
  cookieHeader: string,
  key: string,
  event: string,
  properties: ServerEventProperties,
): ServerCapturePayload | null {
  if (consentFromCookieHeader(cookieHeader)?.analytics !== true) return null;
  const distinctId = postHogDistinctId(cookieHeader, key);
  if (!distinctId) return null;
  return { api_key: key, event, distinct_id: distinctId, properties };
}

function postHogDistinctId(cookieHeader: string, key: string): string | null {
  const name = `ph_${key}_posthog`;
  for (const part of cookieHeader.split(";")) {
    const [cookieName, ...rest] = part.trim().split("=");
    if (cookieName !== name) continue;
    try {
      const id = (JSON.parse(decodeURIComponent(rest.join("="))) as { distinct_id?: unknown }).distinct_id;
      return typeof id === "string" && id ? id : null;
    } catch {
      return null;
    }
  }
  return null;
}
