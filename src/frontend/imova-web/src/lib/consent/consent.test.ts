import { describe, expect, it } from "vitest";
import { consentCookie, consentFromCookieHeader, parseConsent, serializeConsent } from "./consent";

describe("serializeConsent / parseConsent", () => {
  it("round-trips both answers", () => {
    expect(serializeConsent({ analytics: true })).toBe("1:analytics");
    expect(serializeConsent({ analytics: false })).toBe("1:");
    expect(parseConsent("1:analytics")).toEqual({ analytics: true });
    expect(parseConsent("1:")).toEqual({ analytics: false });
  });

  it("ignores categories it doesn't know", () => {
    expect(parseConsent("1:marketing,analytics")).toEqual({ analytics: true });
    expect(parseConsent("1:marketing")).toEqual({ analytics: false });
  });

  it("treats a missing, malformed or older answer as no answer", () => {
    for (const value of [null, undefined, "", "yes", "analytics", "0:analytics", "2:analytics", "1:Analytics", "1:analytics;x"]) {
      expect(parseConsent(value)).toBeNull();
    }
  });
});

describe("consentFromCookieHeader", () => {
  it("finds our cookie among others", () => {
    expect(consentFromCookieHeader("NEXT_LOCALE=ro; imova_consent=1%3Aanalytics; imova_vid=x")).toEqual({ analytics: true });
    expect(consentFromCookieHeader("imova_consent=1%3A")).toEqual({ analytics: false });
    expect(consentFromCookieHeader("NEXT_LOCALE=ro")).toBeNull();
    expect(consentFromCookieHeader("")).toBeNull();
  });
});

describe("consentCookie", () => {
  it("is first-party, a year long, and Secure on https", () => {
    const cookie = consentCookie({ analytics: true }, true);
    expect(cookie).toBe("imova_consent=1%3Aanalytics; Path=/; Max-Age=31536000; SameSite=Lax; Secure");
    expect(consentCookie({ analytics: false }, false)).not.toContain("Secure");
    expect(consentFromCookieHeader(cookie.split(";")[0])).toEqual({ analytics: true });
  });
});
