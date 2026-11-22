import { getLocale, getTranslations } from "next-intl/server";
import { MoldovaMap, type MoldovaMapSpot } from "@/components/home/MoldovaMap";
import { LinkButton } from "@/components/ui/Button";
import { mapSpots } from "@/lib/home/moldovaMap";
import { MAP_PATH, searchHref } from "@/lib/search/filters";
import type { Listing } from "@/types/listing";

// "Caută pe hartă": the map's own section, after the newest listings. A white card — the owners'
// band right under it is already dark — with the invitation on one side and Moldova in dots on the
// other, one spot per raion with listings (sized by how many), each opening that raion on the map.
// On a phone the map comes first, as the picture, and the button last, within thumb reach.
export async function HomeMapSection({ listings }: { listings: Listing[] }) {
  const [t, locale] = await Promise.all([getTranslations("HomeMap"), getLocale()]);
  const numbers = new Intl.NumberFormat(locale);
  const spots: MoldovaMapSpot[] = mapSpots(listings).map((spot) => ({
    ...spot,
    href: searchHref({ raionId: [spot.raionId] }, "map"),
    ariaLabel: t("spotLabel", { place: spot.raionName, count: spot.count }),
    countText: numbers.format(spot.count),
  }));

  return (
    <section className="mx-auto max-w-6xl px-4 pb-12 sm:px-6 sm:pb-16">
      <div className="grid items-center gap-6 rounded-3xl border border-line bg-white px-5 py-7 sm:grid-cols-[1fr_auto] sm:gap-10 sm:px-10 sm:py-9 lg:px-14">
        <div className="flex flex-col items-stretch gap-3 sm:items-start">
          <span className="text-xs font-bold uppercase tracking-[0.09em] text-accent-600">{t("kicker")}</span>
          <h2 className="text-balance font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("title")}</h2>
          <p className="max-w-md text-sm text-ink-500 sm:text-base">{t("text")}</p>
          <LinkButton href={MAP_PATH} className="mt-2 justify-center">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4" aria-hidden>
              <path d="M9 4 3 6v14l6-2 6 2 6-2V4l-6 2-6-2ZM9 4v14M15 6v14" />
            </svg>
            {t("button")}
          </LinkButton>
        </div>
        <MoldovaMap spots={spots} ariaLabel={t("mapLabel")} className="order-first mx-auto w-[160px] sm:order-none sm:w-[210px] lg:w-[240px]" />
      </div>
    </section>
  );
}
