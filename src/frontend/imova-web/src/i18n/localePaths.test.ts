import { describe, expect, it } from "vitest";
import { alternateLinksHeader, localeAlternates, localeRedirect, localizedPath, splitLocale } from "./localePaths";

describe("splitLocale", () => {
  it("takes the language prefix off", () => {
    expect(splitLocale("/ru/search")).toEqual({ locale: "ru", path: "/search" });
    expect(splitLocale("/en")).toEqual({ locale: "en", path: "/" });
  });

  it("leaves a path without one alone, including one that only starts like a language", () => {
    expect(splitLocale("/search")).toEqual({ locale: null, path: "/search" });
    expect(splitLocale("/")).toEqual({ locale: null, path: "/" });
    expect(splitLocale("/russia")).toEqual({ locale: null, path: "/russia" });
  });
});

describe("localizedPath", () => {
  it("keeps Romanian bare and prefixes the others", () => {
    expect(localizedPath("ro", "/search?transactionType=Sale")).toBe("/search?transactionType=Sale");
    expect(localizedPath("ru", "/search?transactionType=Sale")).toBe("/ru/search?transactionType=Sale");
    expect(localizedPath("en", "/")).toBe("/en");
    expect(localizedPath("en", "/?x=1")).toBe("/en?x=1");
  });
});

describe("localeRedirect", () => {
  const visit = { method: "GET", pathname: "/search", search: "?a=1", secFetchDest: "document", cookieLocale: "ru" };

  it("sends an unprefixed page visit to the cookie's language", () => {
    expect(localeRedirect(visit)).toBe("/ru/search?a=1");
    expect(localeRedirect({ ...visit, pathname: "/", search: "" })).toBe("/ru");
    expect(localeRedirect({ ...visit, secFetchDest: null })).toBe("/ru/search?a=1");
  });

  it("does nothing for Romanian, no cookie, an unknown value or an already prefixed address", () => {
    expect(localeRedirect({ ...visit, cookieLocale: "ro" })).toBeNull();
    expect(localeRedirect({ ...visit, cookieLocale: undefined })).toBeNull();
    expect(localeRedirect({ ...visit, cookieLocale: "de" })).toBeNull();
    expect(localeRedirect({ ...visit, pathname: "/en/search" })).toBeNull();
  });

  it("leaves background requests and form posts alone", () => {
    expect(localeRedirect({ ...visit, secFetchDest: "empty" })).toBeNull();
    expect(localeRedirect({ ...visit, secFetchDest: "image" })).toBeNull();
    expect(localeRedirect({ ...visit, method: "POST" })).toBeNull();
  });
});

describe("alternateLinksHeader", () => {
  it("lists the page in every language, plus x-default", () => {
    expect(alternateLinksHeader("https://imova.md", "/search", "?a=1")).toBe(
      '<https://imova.md/search?a=1>; rel="alternate"; hreflang="ro", ' +
        '<https://imova.md/en/search?a=1>; rel="alternate"; hreflang="en", ' +
        '<https://imova.md/ru/search?a=1>; rel="alternate"; hreflang="ru", ' +
        '<https://imova.md/search?a=1>; rel="alternate"; hreflang="x-default"',
    );
  });
});

describe("localeAlternates", () => {
  it("makes the page's own address canonical", () => {
    expect(localeAlternates("ru", "/agencies")).toEqual({
      canonical: "/ru/agencies",
      languages: { ro: "/agencies", en: "/en/agencies", ru: "/ru/agencies", "x-default": "/agencies" },
    });
  });
});
