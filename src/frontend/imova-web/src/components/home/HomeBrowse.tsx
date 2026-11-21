import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { PropertyCard } from "@/components/property/PropertyCard";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { LinkButton } from "@/components/ui/Button";
import { searchHref } from "@/lib/search/filters";
import type { Listing } from "@/types/listing";

const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;

// How many of the newest listings the home page shows under the map.
const NEWEST_COUNT = 8;

// Right under the hero: browse by property type (one tap into that type's results), then the newest
// listings — so the first scroll already shows real homes. `listings` come newest first.
export async function HomeBrowse({ listings, addListingLabel }: { listings: Listing[]; addListingLabel: string }) {
  const t = await getTranslations("Home");
  const tSearch = await getTranslations("Search");
  const newest = listings.slice(0, NEWEST_COUNT);

  return (
    <section className="mx-auto max-w-6xl px-4 pb-14 pt-6 sm:px-6 sm:pt-8">
      <nav aria-label={t("browseByType")} className="-mx-4 overflow-x-auto px-4 [scrollbar-width:none] sm:mx-0 sm:px-0 [&::-webkit-scrollbar]:hidden">
        <ul className="flex min-w-max gap-2 border-b border-ink-100 sm:min-w-0 sm:justify-between sm:gap-4">
          {PROPERTY_TYPES.map((type) => (
            <li key={type}>
              <Link
                href={searchHref({ propertyType: [type] })}
                className="flex flex-col items-center gap-1.5 border-b-2 border-transparent px-3 pb-3 pt-2 text-sm font-medium text-ink-500 transition-colors hover:border-ink-300 hover:text-ink-950"
              >
                <PropertyIcon type={type} className="h-6 w-6" />
                {tSearch(`typePlural.${type}`)}
              </Link>
            </li>
          ))}
        </ul>
      </nav>

      <div className="mb-5 mt-8 flex flex-wrap items-end justify-between gap-3 sm:mt-10">
        <div>
          <h2 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("newestListings")}</h2>
          {listings.length > 0 && (
            <p className="mt-1 text-sm text-ink-500">{t("listingsAvailable", { count: listings.length })}</p>
          )}
        </div>
        {listings.length > 0 && (
          <LinkButton href={searchHref({ sort: ["Newest"] })} variant="secondary" size="sm">
            {t("viewAll")}
          </LinkButton>
        )}
      </div>

      {newest.length > 0 ? (
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {newest.map((listing) => (
            <PropertyCard key={listing.id} listing={listing} />
          ))}
        </div>
      ) : (
        <div className="flex flex-col items-center gap-4 rounded-2xl border border-dashed border-ink-200 bg-white px-6 py-16 text-center">
          <p className="text-sm text-ink-500">{t("emptyStateText")}</p>
          <LinkButton href="/properties/new" size="sm">
            {addListingLabel}
          </LinkButton>
        </div>
      )}
    </section>
  );
}
