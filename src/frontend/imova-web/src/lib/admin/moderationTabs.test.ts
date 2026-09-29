import { describe, expect, it } from "vitest";
import { moderationHref, parseModerationTab, parsePage, statusForTab } from "@/lib/admin/moderationTabs";

describe("parseModerationTab", () => {
  it("reads the known tabs", () => {
    expect(parseModerationTab("active")).toBe("active");
    expect(parseModerationTab("suspended")).toBe("suspended");
  });

  it("falls back to the review queue", () => {
    expect(parseModerationTab(undefined)).toBe("pending");
    expect(parseModerationTab("Draft")).toBe("pending");
  });
});

describe("statusForTab", () => {
  it("maps each tab to the API's listing status", () => {
    expect(statusForTab("pending")).toBe("PendingReview");
    expect(statusForTab("active")).toBe("Active");
    expect(statusForTab("suspended")).toBe("Suspended");
  });
});

describe("moderationHref", () => {
  it("leaves the defaults out", () => {
    expect(moderationHref({ tab: "pending" })).toBe("/admin/moderation");
    expect(moderationHref({ tab: "pending", q: "  ", page: 1 })).toBe("/admin/moderation");
  });

  it("keeps the tab, the trimmed search and the page", () => {
    expect(moderationHref({ tab: "active", q: " Ion Popescu ", page: 3 })).toBe("/admin/moderation?tab=active&q=Ion+Popescu&page=3");
  });
});

describe("parsePage", () => {
  it("reads a positive whole number", () => {
    expect(parsePage("4")).toBe(4);
  });

  it("falls back to page 1", () => {
    expect(parsePage(undefined)).toBe(1);
    expect(parsePage("0")).toBe(1);
    expect(parsePage("-2")).toBe(1);
    expect(parsePage("2.5")).toBe(1);
    expect(parsePage("abc")).toBe(1);
  });
});
