import Link from "next/link";
import { useTranslations } from "next-intl";
import { Badge } from "@/components/ui/Badge";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { SaveListingButton } from "@/components/property/SaveListingButton";
import { formatLocation, formatPrice } from "@/lib/utils/format";
import { cn } from "@/lib/utils/cn";
import { coverPhoto, numberAttribute, photoSrcSet } from "@/lib/listing/view";
import { formatPercentChange } from "@/lib/listing/priceHistory";
import type { Listing } from "@/types/listing";

export function PropertyCard({
  listing,
  showFloor = false,
  hidePerMonthSuffix = false,
}: {
  listing: Listing;
  // Extra bits the cluster-overflow map panel wants (ClusterOverflowPanel.tsx) that the
  // regular search/saved-listings grids don't — kept optional so this stays the one card
  // implementation everywhere instead of a near-duplicate compact card.
  showFloor?: boolean;
  hidePerMonthSuffix?: boolean;
}) {
  const tType = useTranslations("PropertyType");
  const tListing = useTranslations("ListingType");
  const tDetail = useTranslations("PropertyDetail");
  const tCard = useTranslations("PropertyCard");
  const tPrice = useTranslations("PriceHistory");
  const location = formatLocation(listing.property.location);
  const isUnavailable = listing.status !== "Active";
  const reduction = isUnavailable ? null : listing.priceReduction;
  // Rooms/floors live in the per-type attributes now — only some property types have them.
  const rooms = numberAttribute(listing, "rooms");
  const floor = numberAttribute(listing, "floor");
  const totalFloors = numberAttribute(listing, "totalFloors");
  const cover = coverPhoto(listing);

  return (
    <Link
      href={`/property/${listing.id}`}
      className="group flex flex-col overflow-hidden rounded-2xl border border-ink-100 bg-white shadow-[var(--shadow-card)] transition-all hover:-translate-y-0.5 hover:shadow-[var(--shadow-card-hover)]"
    >
      <div className="relative flex aspect-[4/3] items-center justify-center overflow-hidden bg-gradient-to-br from-brand-800 to-brand-600">
        {cover ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={cover.cardUrl}
            srcSet={photoSrcSet(cover, "card")}
            sizes="(min-width: 1024px) 400px, (min-width: 640px) 50vw, 100vw"
            alt={listing.title}
            loading="lazy"
            className={cn(
              "h-full w-full object-cover transition-transform duration-300 group-hover:scale-105",
              isUnavailable && "grayscale",
            )}
          />
        ) : (
          <PropertyIcon
            type={listing.property.propertyType}
            className={cn(
              "h-16 w-16 text-white/25 transition-transform duration-300 group-hover:scale-110",
              isUnavailable && "grayscale",
            )}
          />
        )}
        <div className="absolute left-3 top-3">
          <Badge tone={listing.transactionType === "Rent" ? "accent" : "brand"} className="bg-white/90 backdrop-blur">
            {tListing(listing.transactionType)}
          </Badge>
        </div>

        <div className="absolute right-3 top-3 flex gap-1.5">
          <SaveListingButton listingId={listing.id} initialSaved={listing.isSaved} />
        </div>

        {isUnavailable && (
          <div className="absolute inset-x-0 top-1/2 -translate-y-1/2 bg-ink-950/75 py-1.5 text-center">
            <span className="text-xs font-semibold uppercase tracking-wide text-white">
              {statusOverlayLabel(tCard, listing.status)}
            </span>
          </div>
        )}
      </div>

      <div className={cn("flex flex-1 flex-col gap-2 p-4", isUnavailable && "opacity-60")}>
        <div className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
          <p className="font-display text-xl font-medium text-ink-950">
            {formatPrice(listing.price.amount, listing.price.currency)}
            {listing.transactionType === "Rent" && !hidePerMonthSuffix && (
              <span className="ml-1 text-sm font-normal text-ink-500">{tCard("perMonth")}</span>
            )}
          </p>
          {/* "Preț redus": the old price struck through, and by how much. */}
          {reduction && (
            <p className="flex items-baseline gap-1.5 text-sm">
              <span className="sr-only">{tPrice("previousPrice")}</span>
              <s className="text-ink-400">{formatPrice(reduction.previousAmount, reduction.previousCurrency)}</s>
              <Badge tone="success" className="px-2 py-0.5">
                {formatPercentChange(-reduction.percent)}
              </Badge>
            </p>
          )}
        </div>

        <h3 className="line-clamp-2 text-sm font-medium leading-snug text-ink-800">
          {listing.title}
        </h3>

        <div className="mt-auto flex flex-wrap items-center gap-x-3 gap-y-1 pt-2 text-xs text-ink-500">
          <span>{tType(listing.property.propertyType)}</span>
          <span className="h-1 w-1 rounded-full bg-ink-300" />
          <span>{listing.property.totalAreaM2} m²</span>
          {rooms != null && (
            <>
              <span className="h-1 w-1 rounded-full bg-ink-300" />
              <span>{tCard("rooms", { count: rooms })}</span>
            </>
          )}
          {showFloor && floor != null && (
            <>
              <span className="h-1 w-1 rounded-full bg-ink-300" />
              <span>{totalFloors != null ? tDetail("floorOf", { floor, totalFloors }) : floor}</span>
            </>
          )}
        </div>

        {location && (
          <p className="flex items-center gap-1 text-xs text-ink-400">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-3.5 w-3.5 shrink-0">
              <path d="M12 21s-7-6.1-7-11a7 7 0 1 1 14 0c0 4.9-7 11-7 11Z" />
              <circle cx="12" cy="10" r="2.5" />
            </svg>
            {location}
          </p>
        )}
      </div>
    </Link>
  );
}

function statusOverlayLabel(tCard: ReturnType<typeof useTranslations<"PropertyCard">>, status: string) {
  switch (status) {
    case "Rented":
      return tCard("statusRented");
    case "Sold":
      return tCard("statusSold");
    default:
      return tCard("statusUnavailable");
  }
}
