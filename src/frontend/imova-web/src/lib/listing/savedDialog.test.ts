import { describe, expect, it } from "vitest";
import { savedOutcome } from "./savedDialog";

describe("savedOutcome", () => {
  it("is live for an active listing", () => {
    expect(savedOutcome("Active", false)).toBe("live");
  });

  it("only says resubmitted when a rejected listing really went back to review", () => {
    expect(savedOutcome("PendingReview", true)).toBe("resubmitted");
    expect(savedOutcome("PendingReview", false)).toBe("inReview");
    // Unconfirmed email: the save kept it Rejected.
    expect(savedOutcome("Rejected", true)).toBe("needsEmailToResubmit");
    expect(savedOutcome("Draft", false)).toBe("needsEmail");
  });

  it("is not public for an ended or suspended listing", () => {
    for (const status of ["Suspended", "Expired", "Archived", "Sold", "Rented"]) {
      expect(savedOutcome(status, false)).toBe("notPublic");
    }
  });
});
