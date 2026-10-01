import { describe, expect, it } from "vitest";
import { swipeDirection } from "./swipe";

describe("swipeDirection", () => {
  it("a finger moving left shows the next photo, right the previous", () => {
    expect(swipeDirection(-120, 10)).toBe("next");
    expect(swipeDirection(90, -15)).toBe("previous");
  });

  it("ignores taps, short wobbles and mostly vertical drags", () => {
    expect(swipeDirection(0, 0)).toBeNull();
    expect(swipeDirection(-30, 0)).toBeNull();
    expect(swipeDirection(-80, 120)).toBeNull();
  });
});
