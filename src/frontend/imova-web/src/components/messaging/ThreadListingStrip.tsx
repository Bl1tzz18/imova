import Link from "next/link";
import { useTranslations } from "next-intl";
import { canOpenListing } from "@/lib/messaging/listingStrip";
import { formatPrice } from "@/lib/utils/format";
import { cn } from "@/lib/utils/cn";
import type { ConversationListingDetails } from "@/types/messaging";

// The listing a conversation is about, under the thread header: photo, title and key facts. The
// whole strip opens the listing (with a "Vezi anunțul →" pill as the visible cue) when the viewer
// can open it; a deleted listing is shown as such.
export function ThreadListingStrip({
  listing,
  viewerIsInitiator,
}: {
  listing: ConversationListingDetails | null;
  viewerIsInitiator: boolean;
}) {
  const t = useTranslations("Messages");
  const tType = useTranslations("ListingType");
  const tCard = useTranslations("PropertyCard");

  const photo = (
    <div className="flex h-14 w-14 shrink-0 items-center justify-center overflow-hidden rounded-[12px] bg-bubble text-ink-300">
      {listing?.photoUrl ? (
        // eslint-disable-next-line @next/next/no-img-element -- blob storage URL
        <img src={listing.photoUrl} alt="" className="h-full w-full object-cover" />
      ) : (
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" className="h-6 w-6" aria-hidden>
          <path d="M3.5 11 12 4l8.5 7M5.5 9.5V20h13V9.5M10 20v-5h4v5" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      )}
    </div>
  );

  if (!listing) {
    return (
      <div className="flex items-center gap-3 border-b border-line px-5 py-3">
        {photo}
        <p className="text-sm text-ink-400">{t("listingDeleted")}</p>
      </div>
    );
  }

  const facts = [
    tType(listing.transactionType),
    listing.rooms != null ? tCard("rooms", { count: listing.rooms }) : null,
    `${listing.totalAreaM2} m²`,
    `${formatPrice(listing.priceAmount, listing.priceCurrency)}${listing.transactionType === "Rent" ? tCard("perMonth") : ""}`,
  ].filter(Boolean);
  const openable = canOpenListing(listing, viewerIsInitiator);

  const content = (
    <>
      {photo}
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-semibold text-ink-950">{listing.title}</p>
        <p className="truncate text-xs text-ink-500">{facts.join(" · ")}</p>
      </div>
      {openable ? (
        <span className="flex shrink-0 items-center gap-1.5 rounded-full border border-line bg-white px-3.5 py-1.5 text-sm font-medium text-accent-600 transition-colors group-hover:border-accent-300 group-hover:bg-accent-100/40">
          <span className="hidden sm:inline">{t("viewListing")}</span>
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-4 w-4" aria-hidden>
            <path d="M5 12h14M13 6l6 6-6 6" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </span>
      ) : (
        !listing.isActive && (
          <span className="shrink-0 rounded-full bg-bubble px-3 py-1 text-xs font-medium text-ink-500">{tCard("statusUnavailable")}</span>
        )
      )}
    </>
  );

  const row = "flex items-center gap-3 border-b border-line px-5 py-3";
  return openable ? (
    <Link href={`/property/${listing.id}`} aria-label={`${t("viewListing")}: ${listing.title}`} className={cn(row, "group transition-colors hover:bg-bubble")}>
      {content}
    </Link>
  ) : (
    <div className={row}>{content}</div>
  );
}
