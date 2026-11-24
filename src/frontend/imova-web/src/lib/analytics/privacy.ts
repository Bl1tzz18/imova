// What product analytics (PostHog, components/analytics/AnalyticsProvider) may see. Pure, so Vitest
// covers it. Paths here come without the language prefix unless a function says otherwise.

// Query values that are secrets or identify someone: the one-time links in our emails
// (confirm-email, reset-password, delete-account, unsubscribe) carry them.
const SECRET_PARAMS = new Set(["token", "email", "userid", "user", "code"]);

// The token is the path itself here (/invitations/<token>, any language prefix).
const SECRET_PATHS = /^((?:\/[a-z]{2})?\/invitations\/)[^/?#]+/i;

export const REDACTED = "REDACTED";

// A full or relative URL with secret values replaced. Unparseable input comes back unchanged.
export function redactUrl(url: string): string {
  let parsed: URL;
  try {
    parsed = new URL(url, "http://placeholder.invalid");
  } catch {
    return url;
  }
  for (const key of [...parsed.searchParams.keys()]) {
    if (SECRET_PARAMS.has(key.toLowerCase())) parsed.searchParams.set(key, REDACTED);
  }
  parsed.pathname = parsed.pathname.replace(SECRET_PATHS, `$1${REDACTED}`);
  const relative = !/^[a-z][a-z0-9+.-]*:/i.test(url);
  return relative ? `${parsed.pathname}${parsed.search}${parsed.hash}` : parsed.toString();
}

// Session replay never runs on these pages: private correspondence, account and admin screens,
// and the sign-in / one-time-link pages. Elsewhere inputs are masked anyway.
const NO_REPLAY = [
  "/messages",
  "/account",
  "/admin",
  "/login",
  "/register",
  "/forgot-password",
  "/reset-password",
  "/confirm-email",
  "/delete-account",
  "/invitations",
  "/saved-searches/unsubscribe",
  "/favorites/unsubscribe",
];

export function replayAllowed(pathname: string): boolean {
  const path = pathname.split(/[?#]/)[0];
  return !NO_REPLAY.some((p) => path === p || path.startsWith(`${p}/`));
}
