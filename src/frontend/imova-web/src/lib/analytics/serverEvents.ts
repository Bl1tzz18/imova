import { cookies } from "next/headers";
import type { AnalyticsEvent } from "@/lib/analytics/events";
import { serverCapturePayload, type ServerEventProperties } from "@/lib/analytics/serverCapture";

// PostHog's EU capture endpoint — the same place the browser's /ingest rewrite leads (next.config).
const CAPTURE_URL = "https://eu.i.posthog.com/i/v0/e/";

// An analytics event raised by a server action, for something the browser can't see happen (the
// action redirects straight away). Same rules as track(): only with the visitor's consent to
// analytics cookies, under the browser's own PostHog id, properties are ids and categories only.
// Never throws and gives up quickly — a sign-up must not wait on, or fail because of, analytics.
export async function captureServerEvent(event: AnalyticsEvent, properties: ServerEventProperties = {}) {
  const key = process.env.POSTHOG_KEY?.trim();
  if (!key) return;
  const payload = serverCapturePayload((await cookies()).toString(), key, event, properties);
  if (!payload) return;
  try {
    await fetch(CAPTURE_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
      signal: AbortSignal.timeout(2000),
    });
  } catch {
    // analytics only
  }
}
