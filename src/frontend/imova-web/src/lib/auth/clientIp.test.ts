import { describe, expect, it } from "vitest";
import { clientIpFrom } from "./clientIp";

describe("clientIpFrom", () => {
  it("takes the rightmost X-Forwarded-For entry — the one our own proxy added", () => {
    expect(clientIpFrom("203.0.113.9, 10.0.0.2", null)).toBe("10.0.0.2");
  });

  it("handles a single entry and stray whitespace", () => {
    expect(clientIpFrom("  198.51.100.7 ", null)).toBe("198.51.100.7");
  });

  it("falls back to X-Real-IP", () => {
    expect(clientIpFrom(null, "198.51.100.8")).toBe("198.51.100.8");
    expect(clientIpFrom(" , ", "198.51.100.8")).toBe("198.51.100.8");
  });

  it("returns null when neither header says anything", () => {
    expect(clientIpFrom(null, null)).toBeNull();
    expect(clientIpFrom("", "")).toBeNull();
  });
});
