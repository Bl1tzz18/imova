import { describe, expect, it } from "vitest";
import { REPORT_TEXT_MAX, reportFormProblem, reporterSignal, topReason, waitingFor } from "@/lib/listing/reports";
import type { ListingReporter } from "@/types/listingReport";

const reporter = (overrides: Partial<ListingReporter> = {}): ListingReporter => ({
  userId: "u1",
  displayName: "Maria Rusu",
  email: "maria@example.com",
  isDeleted: false,
  reportsFiled: 2,
  reportsDismissed: 0,
  ...overrides,
});

describe("reportFormProblem", () => {
  it("needs a reason", () => {
    expect(reportFormProblem(null, "")).toBe("reasonRequired");
  });

  it("needs details only for Other", () => {
    expect(reportFormProblem("Other", "   ")).toBe("detailsRequired");
    expect(reportFormProblem("Other", "Fotografiile sunt ale altei case.")).toBeNull();
    expect(reportFormProblem("Fraud", "")).toBeNull();
  });

  it("caps the details like the API", () => {
    expect(reportFormProblem("Fraud", "a".repeat(REPORT_TEXT_MAX))).toBeNull();
    expect(reportFormProblem("Fraud", "a".repeat(REPORT_TEXT_MAX + 1))).toBe("detailsTooLong");
  });
});

describe("reporterSignal", () => {
  it("flags a deleted account first", () => {
    expect(reporterSignal(reporter({ isDeleted: true, reportsFiled: 1 }))).toBe("deleted");
  });

  it("marks a first report", () => {
    expect(reporterSignal(reporter({ reportsFiled: 1 }))).toBe("first");
  });

  it("warns when most of someone's reports were dismissed", () => {
    expect(reporterSignal(reporter({ reportsFiled: 6, reportsDismissed: 3 }))).toBe("oftenDismissed");
    expect(reporterSignal(reporter({ reportsFiled: 7, reportsDismissed: 3 }))).toBeNull();
    // Two dismissed out of two isn't a pattern yet.
    expect(reporterSignal(reporter({ reportsFiled: 3, reportsDismissed: 2 }))).toBeNull();
  });

  it("says nothing about a reporter with a good record", () => {
    expect(reporterSignal(reporter({ reportsFiled: 5, reportsDismissed: 1 }))).toBeNull();
  });
});

describe("waitingFor", () => {
  const now = new Date("2026-10-01T12:00:00Z");

  it("uses the largest whole unit", () => {
    expect(waitingFor("2026-10-01T11:48:00Z", now)).toEqual({ value: 12, unit: "minute" });
    expect(waitingFor("2026-10-01T06:59:00Z", now)).toEqual({ value: 5, unit: "hour" });
    expect(waitingFor("2026-09-28T11:00:00Z", now)).toEqual({ value: 3, unit: "day" });
  });

  it("never goes negative", () => {
    expect(waitingFor("2026-10-01T12:00:30Z", now)).toEqual({ value: 0, unit: "minute" });
  });
});

describe("topReason", () => {
  it("is the first (most frequent) reason, if any", () => {
    expect(topReason([{ reason: "Fraud", count: 3 }, { reason: "Duplicate", count: 1 }])).toBe("Fraud");
    expect(topReason([])).toBeNull();
  });
});
