import { describe, expect, it } from "vitest";
import { adminSectionCount, adminWaitingTotal, badgeText, type AdminSummary } from "./adminSections";

const summary: AdminSummary = { pendingReviewListings: 3, reportedListings: 2, openConversationReports: 1, flaggedMessages: 4 };

describe("admin section counts", () => {
  it("adds listings to review and reported listings under moderation, reports and flags under messaging", () => {
    expect(adminSectionCount(summary, "moderation")).toBe(5);
    expect(adminSectionCount(summary, "messaging")).toBe(5);
    expect(adminSectionCount(summary, "agencies")).toBe(0);
    expect(adminSectionCount(summary, "admins")).toBe(0);
    expect(adminWaitingTotal(summary)).toBe(10);
  });

  it("counts nothing when the summary couldn't be loaded", () => {
    expect(adminWaitingTotal(null)).toBe(0);
    expect(adminSectionCount(null, "moderation")).toBe(0);
  });

  it("caps the badge at 99+", () => {
    expect(badgeText(7)).toBe("7");
    expect(badgeText(99)).toBe("99");
    expect(badgeText(100)).toBe("99+");
  });
});
