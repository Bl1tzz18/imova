import { getTranslations } from "next-intl/server";
import { PropertyCard } from "@/components/property/PropertyCard";
import { LinkButton } from "@/components/ui/Button";
import { SEARCH_PATH } from "@/lib/search/filters";
import type { Listing } from "@/types/listing";

// How many of the newest listings the home page shows: two rows of four on a desktop. A phone shows
// the first PHONE_COUNT, then the "see all" button — eight full-width cards would be a long scroll.
const NEWEST_COUNT = 8;
const PHONE_COUNT = 4;

// Right under the hero: the newest listings, so the first scroll already shows real homes.
// `listings` come newest first.
export async function HomeBrowse({ listings, addListingLabel }: { listings: Listing[]; addListingLabel: string }) {
  const t = await getTranslations("Home");
  const newest = listings.slice(0, NEWEST_COUNT);

  return (
    <section className="mx-auto max-w-6xl px-4 py-12 sm:px-6 sm:py-16">
      <div className="mb-6 flex items-end justify-between gap-4">
        <div>
          <h2 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("newestListings")}</h2>
          {listings.length > 0 && (
            <p className="mt-1 text-sm text-ink-500">{t("listingsAvailable", { count: listings.length })}</p>
          )}
        </div>
        {listings.length > 0 && (
          <div className="hidden sm:block">
            <LinkButton href={SEARCH_PATH} variant="secondary" size="sm">
              {t("viewAll")}
            </LinkButton>
          </div>
        )}
      </div>

      {newest.length > 0 ? (
        <>
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
            {newest.map((listing, i) => (
              <div key={listing.id} className={i >= PHONE_COUNT ? "hidden sm:grid" : "grid"}>
                <PropertyCard listing={listing} />
              </div>
            ))}
          </div>
          <div className="mt-6 sm:hidden">
            <LinkButton href={SEARCH_PATH} variant="secondary" className="w-full">
              {t("viewAll")}
            </LinkButton>
          </div>
        </>
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
