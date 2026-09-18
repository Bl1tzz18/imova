import { cn } from "@/lib/utils/cn";

// A small fixed palette — hash something stable about the user (their id) to a consistent index,
// so the same person always gets the same background color instead of a random one every render.
const PALETTE = ["#1C5C4B", "#1D3557", "#7A3B12", "#4A4E69", "#8D5A97", "#2A6F77", "#B5541E", "#3D5A80"];

function hashToIndex(value: string, mod: number): number {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = (hash * 31 + value.charCodeAt(i)) | 0;
  }
  return Math.abs(hash) % mod;
}

function initialsFor(displayName?: string | null, email?: string | null): string {
  const source = displayName?.trim() || email?.trim() || "?";
  const parts = source.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }
  return source.slice(0, 2).toUpperCase();
}

export function Avatar({
  userId,
  displayName,
  email,
  pictureUrl,
  size = 36,
  className,
}: {
  userId: string;
  displayName?: string | null;
  email?: string | null;
  pictureUrl?: string | null;
  size?: number;
  className?: string;
}) {
  if (pictureUrl) {
    return (
      // Blob-storage URL, not a static asset — same reason PropertyCard uses a plain <img>
      // instead of next/image (no remotePatterns configured for the storage host).
      // eslint-disable-next-line @next/next/no-img-element
      <img
        src={pictureUrl}
        alt=""
        width={size}
        height={size}
        style={{ width: size, height: size }}
        className={cn("rounded-full object-cover", className)}
      />
    );
  }

  return (
    <span
      aria-hidden="true"
      style={{ width: size, height: size, backgroundColor: PALETTE[hashToIndex(userId, PALETTE.length)], fontSize: size * 0.4 }}
      className={cn("flex flex-shrink-0 items-center justify-center rounded-full font-medium text-white", className)}
    >
      {initialsFor(displayName, email)}
    </span>
  );
}
