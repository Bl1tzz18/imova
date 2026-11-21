// Every listing on IMOVA has at least MIN_LISTING_PHOTOS photos — the API's ListingPhotoRules.MinPhotos.
// The form counts the photos that are uploaded and stay (not still uploading, not marked for removal)
// and won't leave the photo step, or save, with fewer. Pure (Vitest-covered).

export const MIN_LISTING_PHOTOS = 3;

// What the form suggests beyond the minimum (no data behind it beyond common sense: every room,
// the outside, the view).
export const RECOMMENDED_LISTING_PHOTOS = 8;

export type PhotoRequirement = {
  count: number;
  // Still to add to reach the minimum (0 once met).
  missing: number;
  // Enough photos, and nothing still on its way up.
  met: boolean;
  uploading: boolean;
};

export function photoRequirement(count: number, uploading = false): PhotoRequirement {
  const missing = Math.max(0, MIN_LISTING_PHOTOS - count);
  return { count, missing, met: missing === 0 && !uploading, uploading };
}

// The photo step's meter: an open-ended bar, so the minimum reads as a threshold to pass, not as the
// number of photos to add. METER_SPAN photos fill the drawn part; the minimum and the recommendation
// are marks along it, and the bar fades out past its end (there's no maximum).
const METER_SPAN = 10;

export type PhotoMeter = {
  // How much of the bar is filled, 0–100.
  fillPercent: number;
  // Where the "minimum" and "recommended" marks sit along the bar, 0–100.
  minPercent: number;
  recommendedPercent: number;
  stage: "belowMinimum" | "enough" | "plenty";
};

export function photoMeter(count: number): PhotoMeter {
  const percent = (n: number) => Math.round((Math.min(Math.max(n, 0), METER_SPAN) / METER_SPAN) * 100);
  return {
    fillPercent: percent(count),
    minPercent: percent(MIN_LISTING_PHOTOS),
    recommendedPercent: percent(RECOMMENDED_LISTING_PHOTOS),
    stage: count < MIN_LISTING_PHOTOS ? "belowMinimum" : count < RECOMMENDED_LISTING_PHOTOS ? "enough" : "plenty",
  };
}
