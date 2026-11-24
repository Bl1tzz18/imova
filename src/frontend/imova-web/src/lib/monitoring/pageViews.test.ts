import { describe, expect, it } from "vitest";
import { deviceOf, pageTemplate, pageViewFrom, sourceOf, type PageRequest } from "./pageViews";

const IPHONE = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1";
const CHROME = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/131.0 Safari/537.36";

function request({ headers: extra, ...overrides }: Partial<Omit<PageRequest, "headers">> & { headers?: Record<string, string> } = {}): PageRequest {
  const headers: Record<string, string> = { "user-agent": CHROME, "sec-fetch-dest": "document", ...extra };
  return {
    method: "GET",
    path: "/search",
    locale: "ro",
    search: "",
    siteHost: "imova.md",
    ...overrides,
    headers: { get: (name: string) => headers[name.toLowerCase()] ?? null },
  };
}

describe("pageTemplate", () => {
  it("names the kind of page, never the record", () => {
    expect(pageTemplate("/")).toBe("/");
    expect(pageTemplate("/search")).toBe("/search");
    expect(pageTemplate("/property/100015-apartament-3-camere")).toBe("/property/[slug]");
    expect(pageTemplate("/agencies/casa-ta-imobiliare")).toBe("/agencies/[slug]");
    expect(pageTemplate("/messages/0c9f1e2a-1111-2222-3333-444455556666")).toBe("/messages/[id]");
    expect(pageTemplate("/messages/new")).toBe("/messages/new");
    expect(pageTemplate("/my-listings/0c9f1e2a-1111/edit")).toBe("/my-listings/[id]/edit");
    expect(pageTemplate("/invitations/secret-token")).toBe("/invitations/[token]");
    expect(pageTemplate("/admin/agencies/0c9f1e2a/import/77")).toBe("/admin");
    expect(pageTemplate("/saved-searches/unsubscribe")).toBe("/saved-searches/unsubscribe");
    expect(pageTemplate("/search/")).toBe("/search");
  });

  it("cuts unknown id-like segments back and lumps unknown sections together", () => {
    expect(pageTemplate("/account/agencies/abc123/members")).toBe("/account/agencies/[id]");
    expect(pageTemplate("/favorites/x9")).toBe("/favorites/[id]");
    expect(pageTemplate("/wp-admin/setup.php")).toBe("other");
  });

  it("isn't a page for downloads and redirects", () => {
    expect(pageTemplate("/message-attachments/abc")).toBeNull();
    expect(pageTemplate("/account/data-export")).toBeNull();
    expect(pageTemplate("/saved-searches/abc/open")).toBeNull();
  });
});

describe("deviceOf / sourceOf", () => {
  it("tells phones, tablets and computers apart", () => {
    expect(deviceOf(IPHONE)).toBe("mobile");
    expect(deviceOf("Mozilla/5.0 (Linux; Android 14; Pixel 8) Chrome/131 Mobile Safari/537.36")).toBe("mobile");
    expect(deviceOf("Mozilla/5.0 (Linux; Android 14; SM-X710) Chrome/131 Safari/537.36")).toBe("tablet");
    expect(deviceOf("Mozilla/5.0 (iPad; CPU OS 18_0 like Mac OS X)")).toBe("tablet");
    expect(deviceOf(CHROME)).toBe("desktop");
  });

  it("keeps only the referring site's domain", () => {
    expect(sourceOf(null, "imova.md")).toBe("direct");
    expect(sourceOf("https://www.google.com/search?q=apartament+chisinau", "imova.md")).toBe("google.com");
    expect(sourceOf("https://m.facebook.com/story.php?id=123", "imova.md")).toBe("m.facebook.com");
    expect(sourceOf("https://imova.md/search", "imova.md")).toBe("internal");
    expect(sourceOf("https://www.imova.md/", "imova.md")).toBe("internal");
    expect(sourceOf("not a url", "imova.md")).toBe("direct");
  });
});

describe("pageViewFrom", () => {
  it("describes a page load with nothing that identifies the visitor", () => {
    const view = pageViewFrom(
      request({
        path: "/property/100015-x",
        locale: "ru",
        search: "?utm_source=Facebook&utm_medium=cpc&utm_campaign=Lansare%20Noiembrie&fbclid=abc",
        headers: { "user-agent": IPHONE, referer: "https://l.facebook.com/l.php?u=x" },
      }),
    );
    expect(view).toEqual({
      page: "/property/[slug]",
      locale: "ru",
      device: "mobile",
      source: "l.facebook.com",
      utmSource: "facebook",
      utmMedium: "cpc",
      utmCampaign: "lansare noiembrie",
    });
  });

  it("counts a full load from an IMOVA page as internal", () => {
    const view = pageViewFrom(request({ headers: { referer: "https://imova.md/" } }));
    expect(view?.source).toBe("internal");
    expect(view?.page).toBe("/search");
  });

  it("counts an old browser without Sec-Fetch headers when it asks for HTML", () => {
    expect(pageViewFrom(request({ headers: { "sec-fetch-dest": "", accept: "text/html,application/xhtml+xml" } }))?.page).toBe("/search");
    expect(pageViewFrom(request({ headers: { "sec-fetch-dest": "", accept: "*/*" } }))).toBeNull();
  });

  it("ignores in-site fetches and prefetches, server actions, bots and non-pages", () => {
    // Next's click navigations and link prefetches reach middleware as plain fetches.
    expect(pageViewFrom(request({ headers: { "sec-fetch-dest": "empty" } }))).toBeNull();
    expect(pageViewFrom(request({ headers: { "sec-purpose": "prefetch;prerender" } }))).toBeNull();
    expect(pageViewFrom(request({ method: "POST" }))).toBeNull();
    expect(pageViewFrom(request({ headers: { "user-agent": "Mozilla/5.0 (compatible; Googlebot/2.1)" } }))).toBeNull();
    expect(pageViewFrom(request({ headers: { "user-agent": "" } }))).toBeNull();
    expect(pageViewFrom(request({ headers: { "sec-fetch-dest": "image" } }))).toBeNull();
    expect(pageViewFrom(request({ path: "/account/data-export" }))).toBeNull();
  });
});
