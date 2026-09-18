import Link from "next/link";
import { useTranslations } from "next-intl";
import { Badge } from "@/components/ui/Badge";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { SaveListingButton } from "@/components/property/SaveListingButton";
import { formatLocation, formatPrice } from "@/lib/utils/format";
import type { Property } from "@/types/property";

export function PropertyCard({ property }: { property: Property }) {
  const tType = useTranslations("PropertyType");
  const tListing = useTranslations("ListingType");
  const tCard = useTranslations("PropertyCard");
  const location = formatLocation(property.location);

  return (
    <Link
      href={`/property/${property.id}`}
      className="group flex flex-col overflow-hidden rounded-2xl border border-ink-100 bg-white shadow-[var(--shadow-card)] transition-all hover:-translate-y-0.5 hover:shadow-[var(--shadow-card-hover)]"
    >
      <div className="relative flex aspect-[4/3] items-center justify-center overflow-hidden bg-gradient-to-br from-brand-800 to-brand-600">
        {property.media.length > 0 ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={property.media[0].url}
            alt={property.title}
            loading="lazy"
            className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
          />
        ) : (
          <PropertyIcon
            type={property.propertyType}
            className="h-16 w-16 text-white/25 transition-transform duration-300 group-hover:scale-110"
          />
        )}
        <div className="absolute left-3 top-3">
          <Badge tone={property.listingType === "Rent" ? "accent" : "brand"} className="bg-white/90 backdrop-blur">
            {tListing(property.listingType)}
          </Badge>
        </div>

        <SaveListingButton
          propertyId={property.id}
          initialSaved={property.isSaved}
          className="absolute right-3 top-3"
        />
      </div>

      <div className="flex flex-1 flex-col gap-2 p-4">
        <p className="font-display text-xl font-medium text-ink-950">
          {formatPrice(property.price, property.currency)}
          {property.listingType === "Rent" && (
            <span className="ml-1 text-sm font-normal text-ink-500">{tCard("perMonth")}</span>
          )}
        </p>

        <h3 className="line-clamp-2 text-sm font-medium leading-snug text-ink-800">
          {property.title}
        </h3>

        <div className="mt-auto flex flex-wrap items-center gap-x-3 gap-y-1 pt-2 text-xs text-ink-500">
          <span>{tType(property.propertyType)}</span>
          {property.area != null && (
            <>
              <span className="h-1 w-1 rounded-full bg-ink-300" />
              <span>{property.area} m²</span>
            </>
          )}
          {property.rooms != null && (
            <>
              <span className="h-1 w-1 rounded-full bg-ink-300" />
              <span>{tCard("rooms", { count: property.rooms })}</span>
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
