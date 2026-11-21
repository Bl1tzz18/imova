import { describe, expect, it } from "vitest";
import { MIN_LISTING_PHOTOS, photoMeter, photoRequirement } from "./photos";

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

describe("photoMeter", () => {
  it("marks the minimum and the recommendation as points along the bar, not its end", () => {
    const meter = photoMeter(0);
    expect(meter.minPercent).toBe(30);
    expect(meter.recommendedPercent).toBe(80);
    expect(meter.fillPercent).toBe(0);
  });

  it("goes from below the minimum, to enough, to plenty", () => {
    expect(photoMeter(2)).toMatchObject({ fillPercent: 20, stage: "belowMinimum" });
    expect(photoMeter(3)).toMatchObject({ fillPercent: 30, stage: "enough" });
    expect(photoMeter(7).stage).toBe("enough");
    expect(photoMeter(8)).toMatchObject({ fillPercent: 80, stage: "plenty" });
  });

  it("stays within the bar however many photos there are", () => {
    expect(photoMeter(25).fillPercent).toBe(100);
    expect(photoMeter(-1).fillPercent).toBe(0);
  });
});
