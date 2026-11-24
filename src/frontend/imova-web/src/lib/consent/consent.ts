// The visitor's cookie choice (components/consent/ConsentBanner). Stored in our own first-party
// cookie, which is strictly necessary (it only remembers the answer), so it needs no consent itself.
// Value: "<version>:<accepted categories, comma-separated>" — "1:analytics" accepted, "1:" refused.
// Categories are a list so marketing (Google Ads / GA4, later) can join without a new format.
// Bump CONSENT_VERSION when what a category covers changes in substance: everyone is asked again.

export const CONSENT_COOKIE = "imova_consent";
export const CONSENT_VERSION = 1;
// Asked again after a year, like the other long-lived cookies.
export const CONSENT_MAX_AGE_SECONDS = 365 * 24 * 60 * 60;

export const CONSENT_CATEGORIES = ["analytics"] as const;
export type ConsentCategory = (typeof CONSENT_CATEGORIES)[number];
export type Consent = Record<ConsentCategory, boolean>;

// Browser events: the choice changed (detail: Consent), or something asks to show the banner again
// (the footer's "Setări cookie-uri").
export const CONSENT_CHANGED_EVENT = "imova:consent-changed";
export const OPEN_CONSENT_EVENT = "imova:open-consent";

export function serializeConsent(consent: Consent): string {
  return `${CONSENT_VERSION}:${CONSENT_CATEGORIES.filter((c) => consent[c]).join(",")}`;
}

// Null = no answer yet (missing, malformed, or given for an older version): show the banner.
export function parseConsent(value: string | null | undefined): Consent | null {
  const match = /^(\d+):([a-z,]*)$/.exec(value ?? "");
  if (!match || Number(match[1]) !== CONSENT_VERSION) return null;
  const accepted = new Set(match[2].split(",").filter(Boolean));
  return Object.fromEntries(CONSENT_CATEGORIES.map((c) => [c, accepted.has(c)])) as Consent;
}

export function consentFromCookieHeader(cookieHeader: string): Consent | null {
  for (const part of cookieHeader.split(";")) {
    const [name, ...rest] = part.trim().split("=");
    if (name === CONSENT_COOKIE) return parseConsent(decodeURIComponent(rest.join("=")));
  }
  return null;
}

export function consentCookie(consent: Consent, secure: boolean): string {
  return `${CONSENT_COOKIE}=${encodeURIComponent(serializeConsent(consent))}; Path=/; Max-Age=${CONSENT_MAX_AGE_SECONDS}; SameSite=Lax${secure ? "; Secure" : ""}`;
}

// Browser only.
export function readConsent(): Consent | null {
  return consentFromCookieHeader(document.cookie);
}

export function saveConsent(consent: Consent): void {
  document.cookie = consentCookie(consent, window.location.protocol === "https:");
  window.dispatchEvent(new CustomEvent<Consent>(CONSENT_CHANGED_EVENT, { detail: consent }));
}
