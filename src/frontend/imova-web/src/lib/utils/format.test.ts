import { describe, expect, it } from "vitest";
import { formatDate } from "./format";

describe("formatDate", () => {
  it("names Moldova's day, not the server's", () => {
    // 22:30 UTC on 30 September is already 1 October in Chișinău (UTC+3).
    expect(formatDate("2026-09-30T22:30:00Z", "en")).toBe("October 1, 2026");
    expect(formatDate("2026-09-30T20:30:00Z", "en")).toBe("September 30, 2026");
  });
});
