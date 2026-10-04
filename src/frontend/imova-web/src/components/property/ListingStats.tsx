import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils/cn";
import type { Listing } from "@/types/listing";

// "ID 100015 · 2 vizualizări · 1 persoană a văzut numărul · salvat de 3 persoane" — a listing's statistics where the owner or
// an admin looks at it (owner bar, "Anunțurile mele", the admin line on the listing, moderation). The
// counts are only in the API's answer for them (null for anyone else, then only the ID shows).
export function ListingStats({
  listing,
  showNumber = false,
  className,
}: {
  listing: Pick<Listing, "number" | "viewCount" | "phoneRevealCount" | "favoriteCount">;
  showNumber?: boolean;
  className?: string;
}) {
  const t = useTranslations("ListingStats");
  const parts = [
    showNumber ? t("number", { number: listing.number }) : null,
    listing.viewCount != null ? t("views", { count: listing.viewCount }) : null,
    listing.phoneRevealCount != null ? t("phoneReveals", { count: listing.phoneRevealCount }) : null,
    listing.favoriteCount != null ? t("favorites", { count: listing.favoriteCount }) : null,
  ].filter(Boolean);

  return parts.length > 0 ? <p className={cn("text-xs text-ink-500", className)}>{parts.join(" · ")}</p> : null;
}
