// Which picked files can be attached to a message — same image types and 10MB limit as listing
// photos (the backend re-checks the actual bytes).
export const ATTACHMENT_ACCEPT = "image/jpeg,image/png,image/webp";
export const MAX_ATTACHMENT_BYTES = 10 * 1024 * 1024;

const EXTENSIONS: Record<string, string> = { "image/jpeg": ".jpg", "image/png": ".png", "image/webp": ".webp" };

// A resized JPEG copy the API makes of a message image: "thumbnail" (≤800 px, the picture in a
// conversation) or "large" (≤1600 px, the full-screen viewer). Without one, the original upload.
export type AttachmentSize = "thumbnail" | "large";

// Where the browser loads a message image: the site's own access-checked proxy
// (app/message-attachments/[id]/route.ts), never a storage URL — the images are private.
export function attachmentSrc(attachmentId: string, size?: AttachmentSize): string {
  const path = `/message-attachments/${encodeURIComponent(attachmentId)}`;
  return size ? `${path}?size=${size}` : path;
}

// The proxy passes on only a size the API knows; anything else means the original.
export function attachmentSizeParam(value: string | null): AttachmentSize | null {
  return value === "thumbnail" || value === "large" ? value : null;
}

export function attachmentExtension(file: { type: string }): string | null {
  return EXTENSIONS[file.type] ?? null;
}

type AttachmentProblem = "type" | "size" | "count";

// Splits picked files into the ones to upload (up to the remaining slots) and why others were
// skipped.
export function pickAttachments<T extends { type: string; size: number }>(
  files: readonly T[],
  alreadyAttached: number,
  maxAttachments: number,
): { accepted: T[]; problems: AttachmentProblem[] } {
  const accepted: T[] = [];
  const problems = new Set<AttachmentProblem>();
  for (const file of files) {
    if (!attachmentExtension(file)) problems.add("type");
    else if (file.size > MAX_ATTACHMENT_BYTES) problems.add("size");
    else if (alreadyAttached + accepted.length >= maxAttachments) problems.add("count");
    else accepted.push(file);
  }
  return { accepted, problems: [...problems] };
}
