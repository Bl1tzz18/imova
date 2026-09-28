// The session's two httpOnly cookies, shared by the server actions (lib/auth/session.ts) and the
// middleware that renews the login token (src/middleware.ts). Pure — no next/headers — so both
// can use it and Vitest can test it.

// The short-lived (15 min) login token, sent to the API as "Bearer".
export const SESSION_COOKIE = "imova_token";
// The refresh token that renews it (POST /api/v1/auth/refresh).
export const REFRESH_COOKIE = "imova_refresh";

// Renew the login token this long before it runs out, so a request never starts with one that
// expires halfway through.
export const REFRESH_MARGIN_MS = 60_000;

export type SessionTokens = {
  token: string;
  expiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  // "Ține-mă minte": lasting cookies. Otherwise they end with the browser (no expiry date).
  persistent: boolean;
};

export type CookieOptions = {
  httpOnly: true;
  sameSite: "lax";
  secure: boolean;
  path: "/";
  expires?: Date;
};

export function sessionCookies(
  session: SessionTokens,
  secure: boolean,
): { name: string; value: string; options: CookieOptions }[] {
  const base = { httpOnly: true, sameSite: "lax", secure, path: "/" } as const;
  return [
    {
      name: SESSION_COOKIE,
      value: session.token,
      options: session.persistent ? { ...base, expires: new Date(session.expiresAt) } : base,
    },
    {
      name: REFRESH_COOKIE,
      value: session.refreshToken,
      options: session.persistent ? { ...base, expires: new Date(session.refreshTokenExpiresAt) } : base,
    },
  ];
}

// When the login token expires, read from its own "exp" claim (seconds). Not verified — only used
// to decide whether to renew it; the API verifies every token it gets. Null if it can't be read.
export function tokenExpiry(token: string): number | null {
  const payload = token.split(".")[1];
  if (!payload) {
    return null;
  }

  try {
    const json = JSON.parse(Buffer.from(payload, "base64url").toString("utf8")) as { exp?: unknown };
    return typeof json.exp === "number" ? json.exp * 1000 : null;
  } catch {
    return null;
  }
}

// Missing, unreadable, expired or about to expire → renew before handling the request.
export function needsRefresh(token: string | undefined, now: number): boolean {
  if (!token) {
    return true;
  }

  const expiry = tokenExpiry(token);
  return expiry === null || expiry - now <= REFRESH_MARGIN_MS;
}
