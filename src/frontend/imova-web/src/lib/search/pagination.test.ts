import { describe, expect, it } from "vitest";
import { pageWindow } from "@/lib/search/pagination";

describe("pageWindow", () => {
  it("shows nothing for a single page", () => {
    expect(pageWindow(1, 1)).toEqual([]);
    expect(pageWindow(1, 0)).toEqual([]);
  });

  it("shows every page when there are few", () => {
    expect(pageWindow(2, 3)).toEqual([1, 2, 3]);
  });

  it("keeps the first, last and neighbours of the current page, with gaps", () => {
    expect(pageWindow(6, 12)).toEqual([1, null, 5, 6, 7, null, 12]);
    expect(pageWindow(1, 12)).toEqual([1, 2, null, 12]);
    expect(pageWindow(12, 12)).toEqual([1, null, 11, 12]);
    expect(pageWindow(3, 12)).toEqual([1, 2, 3, 4, null, 12]);
  });
});
