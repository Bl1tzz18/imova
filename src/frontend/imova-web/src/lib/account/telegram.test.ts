import { describe, expect, it } from "vitest";
import { telegramDisplayName, telegramLinkExpired } from "./telegram";

describe("telegramDisplayName", () => {
  it("prefers the username", () => {
    expect(telegramDisplayName({ username: "ion_p", firstName: "Ion" })).toBe("@ion_p");
  });

  it("falls back to the first name, then to nothing", () => {
    expect(telegramDisplayName({ username: null, firstName: " Ion " })).toBe("Ion");
    expect(telegramDisplayName({ username: null, firstName: "  " })).toBeNull();
    expect(telegramDisplayName({ username: null, firstName: null })).toBeNull();
  });
});

describe("telegramLinkExpired", () => {
  const link = { url: "https://t.me/ImovaDevBot?start=x", expiresAt: "2026-11-22T10:15:00Z" };

  it("is usable until its expiry", () => {
    expect(telegramLinkExpired(link, new Date("2026-11-22T10:14:59Z"))).toBe(false);
    expect(telegramLinkExpired(link, new Date("2026-11-22T10:15:00Z"))).toBe(true);
  });
});
