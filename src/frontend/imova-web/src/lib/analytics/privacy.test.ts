import { describe, expect, it } from "vitest";
import { redactUrl, replayAllowed } from "./privacy";

describe("redactUrl", () => {
  it("hides tokens, emails and user ids from the emailed links", () => {
    expect(redactUrl("https://imova.md/reset-password?email=a%40b.md&token=abc")).toBe(
      "https://imova.md/reset-password?email=REDACTED&token=REDACTED",
    );
    expect(redactUrl("https://imova.md/ru/confirm-email?userId=1f2e&token=xyz")).toBe(
      "https://imova.md/ru/confirm-email?userId=REDACTED&token=REDACTED",
    );
    expect(redactUrl("https://imova.md/favorites/unsubscribe?user=1f2e&token=t")).toBe(
      "https://imova.md/favorites/unsubscribe?user=REDACTED&token=REDACTED",
    );
  });

  it("hides an invitation token in the path, with or without a language", () => {
    expect(redactUrl("https://imova.md/invitations/secret123")).toBe("https://imova.md/invitations/REDACTED");
    expect(redactUrl("https://imova.md/en/invitations/secret123?x=1")).toBe("https://imova.md/en/invitations/REDACTED?x=1");
  });

  it("keeps search filters and ordinary pages as they are", () => {
    const url = "https://imova.md/search?transactionType=Sale&propertyType=Apartment&raionId=5";
    expect(redactUrl(url)).toBe(url);
    expect(redactUrl("https://imova.md/property/100015-apartament")).toBe("https://imova.md/property/100015-apartament");
  });

  it("works on relative URLs", () => {
    expect(redactUrl("/delete-account?userId=1&token=2")).toBe("/delete-account?userId=REDACTED&token=REDACTED");
    expect(redactUrl("/search?page=2")).toBe("/search?page=2");
  });
});

describe("replayAllowed", () => {
  it("is off for private and one-time-link pages", () => {
    for (const path of ["/messages", "/messages/abc", "/account", "/admin/moderation", "/login", "/reset-password", "/invitations/x", "/saved-searches/unsubscribe"]) {
      expect(replayAllowed(path)).toBe(false);
    }
  });

  it("is on for public browsing and the listing form", () => {
    for (const path of ["/", "/search", "/map", "/property/100015-x", "/properties/new", "/saved-searches", "/accounting"]) {
      expect(replayAllowed(path)).toBe(true);
    }
  });
});
