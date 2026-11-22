import { getTranslations } from "next-intl/server";
import { PropertyCarousel } from "@/components/property/PropertyCarousel";
import { LinkButton } from "@/components/ui/Button";
import { SEARCH_PATH } from "@/lib/search/filters";
import type { Listing } from "@/types/listing";

// How many of the newest listings the home page's row holds: three desktop views of four.
const NEWEST_COUNT = 12;

// Right under the hero: the newest listings as one swipeable row (PropertyCarousel, like "Anunțuri
// asemănătoare" on a listing page), so the first scroll already shows real homes without a long
// block of cards — on a phone a stack of full-width cards would push everything else far down.
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
          <PropertyCarousel listings={newest} desktopPerView={4} />
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
