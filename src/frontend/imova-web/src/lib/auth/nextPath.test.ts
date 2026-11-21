import { describe, expect, it } from "vitest";
import { safeNextPath } from "./nextPath";

describe("safeNextPath", () => {
  it("keeps a path on this site, query and hash included", () => {
    expect(safeNextPath("/messages/new?listing=abc#top")).toBe("/messages/new?listing=abc#top");
    expect(safeNextPath("/")).toBe("/");
  });

  it("falls back to the home page when there's nothing usable", () => {
    expect(safeNextPath(undefined)).toBe("/");
    expect(safeNextPath(null)).toBe("/");
    expect(safeNextPath("")).toBe("/");
    expect(safeNextPath(["/account"])).toBe("/");
  });

  it("refuses anything that leaves the site", () => {
    expect(safeNextPath("https://evil.com")).toBe("/");
    expect(safeNextPath("//evil.com")).toBe("/");
    expect(safeNextPath("/\\evil.com")).toBe("/");
    expect(safeNextPath("/\t/evil.com")).toBe("/");
    expect(safeNextPath("/\n/evil.com")).toBe("/");
    expect(safeNextPath("javascript:alert(1)")).toBe("/");
  });
});
