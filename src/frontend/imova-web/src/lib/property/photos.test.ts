import { describe, expect, it } from "vitest";
import { MIN_LISTING_PHOTOS, photoRequirement } from "./photos";

describe("photoRequirement", () => {
  it("asks for three photos, the same as the API", () => {
    expect(MIN_LISTING_PHOTOS).toBe(3);
    expect(photoRequirement(0)).toEqual({ count: 0, missing: 3, met: false, uploading: false });
    expect(photoRequirement(2)).toEqual({ count: 2, missing: 1, met: false, uploading: false });
  });

  it("is met from three on", () => {
    expect(photoRequirement(3).met).toBe(true);
    expect(photoRequirement(12)).toEqual({ count: 12, missing: 0, met: true, uploading: false });
  });

  it("waits for photos still uploading", () => {
    expect(photoRequirement(3, true).met).toBe(false);
    expect(photoRequirement(3, true).missing).toBe(0);
  });
});
