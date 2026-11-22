import { describe, expect, it } from "vitest";
import { redactAttribute, redactUrl, redactSpan } from "./redact";

describe("redactUrl", () => {
  it.each([
    ["/reset-password?email=a@b.md&token=xyz", "/reset-password?email=REDACTED&token=REDACTED"],
    ["/ru/confirm-email?userId=1&token=abc#done", "/ru/confirm-email?userId=REDACTED&token=REDACTED#done"],
    ["http://backend:8080/api/v1/listings/search?raionId=5&maxPriceEur=90000", "http://backend:8080/api/v1/listings/search?raionId=REDACTED&maxPriceEur=REDACTED"],
    ["/search?flag", "/search?flag=REDACTED"],
  ])("keeps only the parameter names of %s", (url, expected) => {
    expect(redactUrl(url)).toBe(expected);
  });

  it("leaves an address without a query alone", () => {
    expect(redactUrl("/property/100015-apartament-botanica")).toBe("/property/100015-apartament-botanica");
  });
});

describe("redactAttribute", () => {
  it("handles url.query with and without its question mark", () => {
    expect(redactAttribute("url.query", "token=abc&id=1")).toBe("token=REDACTED&id=REDACTED");
    expect(redactAttribute("url.query", "?token=abc")).toBe("?token=REDACTED");
    expect(redactAttribute("http.target", "/delete-account?token=abc")).toBe("/delete-account?token=REDACTED");
  });
});

describe("redactSpan", () => {
  it("strips query values from the name and every address attribute, leaving the rest", () => {
    const span = {
      name: "GET /reset-password?email=a@b.md&token=xyz",
      attributes: {
        "url.full": "http://localhost:3000/reset-password?token=xyz",
        "url.query": "token=xyz",
        "http.target": "/reset-password?token=xyz",
        "next.span_name": "GET /reset-password?token=xyz",
        "http.method": "GET",
      } as Record<string, unknown>,
    };

    redactSpan(span);

    expect(span.name).toBe("GET /reset-password?email=REDACTED&token=REDACTED");
    expect(span.attributes).toEqual({
      "url.full": "http://localhost:3000/reset-password?token=REDACTED",
      "url.query": "token=REDACTED",
      "http.target": "/reset-password?token=REDACTED",
      "next.span_name": "GET /reset-password?token=REDACTED",
      "http.method": "GET",
    });
  });
});
