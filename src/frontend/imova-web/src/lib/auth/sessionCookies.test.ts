import { describe, expect, it } from "vitest";
import { needsRefresh, REFRESH_COOKIE, SESSION_COOKIE, sessionCookies, tokenExpiry } from "./sessionCookies";

function jwtExpiringAt(ms: number): string {
  const payload = Buffer.from(JSON.stringify({ sub: "u1", exp: Math.floor(ms / 1000) })).toString("base64url");
  return `header.${payload}.signature`;
}

const now = Date.parse("2026-09-29T12:00:00Z");

describe("tokenExpiry", () => {
  it("reads the exp claim in milliseconds", () => {
    expect(tokenExpiry(jwtExpiringAt(now + 15 * 60_000))).toBe(now + 15 * 60_000);
  });

  it("is null for something that isn't a readable token", () => {
    expect(tokenExpiry("garbage")).toBeNull();
    expect(tokenExpiry("a.%%%.c")).toBeNull();
    expect(tokenExpiry(`a.${Buffer.from("{}").toString("base64url")}.c`)).toBeNull();
  });
});

describe("needsRefresh", () => {
  it("keeps a token with more than a minute left", () => {
    expect(needsRefresh(jwtExpiringAt(now + 10 * 60_000), now)).toBe(false);
  });

  it("renews one in its last minute, an expired one, a missing one and an unreadable one", () => {
    expect(needsRefresh(jwtExpiringAt(now + 30_000), now)).toBe(true);
    expect(needsRefresh(jwtExpiringAt(now - 1), now)).toBe(true);
    expect(needsRefresh(undefined, now)).toBe(true);
    expect(needsRefresh("garbage", now)).toBe(true);
  });
});

describe("sessionCookies", () => {
  const session = {
    token: "access",
    expiresAt: "2026-09-29T12:15:00Z",
    refreshToken: "refresh",
    refreshTokenExpiresAt: "2026-10-29T12:00:00Z",
    persistent: true,
  };

  it("gives a remembered session lasting cookies", () => {
    const [access, refresh] = sessionCookies(session, true);
    expect(access).toMatchObject({ name: SESSION_COOKIE, value: "access" });
    expect(access.options.expires).toEqual(new Date("2026-09-29T12:15:00Z"));
    expect(refresh).toMatchObject({ name: REFRESH_COOKIE, value: "refresh" });
    expect(refresh.options.expires).toEqual(new Date("2026-10-29T12:00:00Z"));
    expect(refresh.options).toMatchObject({ httpOnly: true, sameSite: "lax", secure: true, path: "/" });
  });

  it("gives any other session cookies that end with the browser", () => {
    for (const cookie of sessionCookies({ ...session, persistent: false }, false)) {
      expect(cookie.options.expires).toBeUndefined();
      expect(cookie.options.secure).toBe(false);
    }
  });
});
