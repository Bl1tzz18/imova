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
