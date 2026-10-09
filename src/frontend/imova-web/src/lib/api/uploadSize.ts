// The size limits of the single-image uploads (profile picture, agency logo) — the same numbers the
// API checks, so the page, the route handler and the API refuse the same files.

export const PROFILE_PICTURE_MAX_BYTES = 5 * 1024 * 1024;
export const AGENCY_LOGO_MAX_BYTES = 5 * 1024 * 1024;

// Multipart framing (boundaries, headers, the file name) on top of the file itself.
export const MULTIPART_OVERHEAD_BYTES = 64 * 1024;

// "tooLarge" when the file (or, before reading it, the request's Content-Length) is over the
// limit; null when it fits or nothing is known yet.
export function uploadSizeProblem({
  fileSize,
  contentLength,
  maxBytes,
}: {
  fileSize?: number;
  contentLength?: string | null;
  maxBytes: number;
}): "tooLarge" | null {
  if (fileSize !== undefined && fileSize > maxBytes) return "tooLarge";
  const length = contentLength ? Number(contentLength) : NaN;
  if (Number.isFinite(length) && length > maxBytes + MULTIPART_OVERHEAD_BYTES) return "tooLarge";
  return null;
}
