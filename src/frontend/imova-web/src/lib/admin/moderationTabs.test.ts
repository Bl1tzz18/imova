import { describe, expect, it } from "vitest";
import { isListingTab, moderationHref, parseModerationTab, parsePage, parseReportView, statusForTab } from "@/lib/admin/moderationTabs";

describe("parseModerationTab", () => {
  it("reads the known tabs", () => {
    expect(parseModerationTab("active")).toBe("active");
    expect(parseModerationTab("suspended")).toBe("suspended");
    expect(parseModerationTab("reports")).toBe("reports");
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

describe("isListingTab", () => {
  it("is every tab but the reports", () => {
    expect(isListingTab("pending")).toBe(true);
    expect(isListingTab("suspended")).toBe(true);
    expect(isListingTab("reports")).toBe(false);
  });
});

describe("parseReportView", () => {
  it("reads the history, and falls back to the open cases", () => {
    expect(parseReportView("resolved")).toBe("resolved");
    expect(parseReportView("open")).toBe("open");
    expect(parseReportView(undefined)).toBe("open");
    expect(parseReportView("all")).toBe("open");
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

  it("keeps the reports view (open is the default) and never a search there", () => {
    expect(moderationHref({ tab: "reports" })).toBe("/admin/moderation?tab=reports");
    expect(moderationHref({ tab: "reports", view: "open", q: "x" })).toBe("/admin/moderation?tab=reports");
    expect(moderationHref({ tab: "reports", view: "resolved", page: 2 })).toBe("/admin/moderation?tab=reports&view=resolved&page=2");
    expect(moderationHref({ tab: "active", view: "resolved" })).toBe("/admin/moderation?tab=active");
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
