import { afterEach, describe, expect, it } from "vitest";
import type { PostHog } from "posthog-js";
import { attachAnalyticsClient, searchEventProperties, track } from "./events";

describe("searchEventProperties", () => {
  it("names the filters used and keeps the coarse values", () => {
    const query = new URLSearchParams("transactionType=Sale&propertyType=Apartment&propertyType=House&raionId=5&minPriceEur=50000&rooms=2&page=3&sort=PriceAsc");
    expect(searchEventProperties(query, "list")).toEqual({
      view: "list",
      filters: ["minPriceEur", "propertyType", "raionId", "rooms", "transactionType"],
      filter_count: 5,
      transactionType: "Sale",
      propertyType: ["Apartment", "House"],
      raionId: "5",
    });
  });

  it("is an empty search with no filters", () => {
    expect(searchEventProperties(new URLSearchParams("page=2"), "map")).toEqual({ view: "map", filters: [], filter_count: 0 });
  });
});

describe("track", () => {
  const setCookie = (cookie: string) => Object.assign(globalThis, { document: { cookie } });
  afterEach(() => {
    delete (globalThis as { document?: unknown }).document;
  });

  it("queues events until the client is attached, then sends them in order — only with consent", () => {
    const sent: [string, unknown][] = [];
    track("search_performed", { view: "list" }); // no document (server): dropped
    setCookie("imova_consent=1%3A");
    track("favorite_removed", { listing_id: "x" }); // refused: dropped, never sent later
    setCookie("imova_consent=1%3Aanalytics");
    track("listing_viewed", { listing_id: "a" });
    track("phone_revealed", { listing_id: "a" });
    attachAnalyticsClient({ capture: (event: string, props: unknown) => void sent.push([event, props]) } as unknown as PostHog);
    track("favorite_added", { listing_id: "b" });
    expect(sent).toEqual([
      ["listing_viewed", { listing_id: "a" }],
      ["phone_revealed", { listing_id: "a" }],
      ["favorite_added", { listing_id: "b" }],
    ]);
  });
});
