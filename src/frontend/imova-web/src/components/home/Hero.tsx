import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { HeroSearchCard } from "@/components/home/HeroSearchCard";
import { MoldovaMap, type MoldovaMapSpot } from "@/components/home/MoldovaMap";
import { MAP_PATH } from "@/lib/search/filters";

// The home page's first screen: Moldova itself, as a map of where the listings are, next to the
// one search question (buy or rent, and where). Two ways in — type a place, or tap one on the map —
// and nothing that only says "real estate" in general. On a phone the search comes first and the
// map follows it.
export async function Hero({ spots }: { spots: MoldovaMapSpot[] }) {
  const t = await getTranslations("Hero");

  return (
    <section className="bg-[radial-gradient(120%_90%_at_85%_40%,var(--color-brand-800),var(--color-brand-950)_70%)] text-white">
      <div className="mx-auto grid max-w-6xl items-center gap-8 px-4 pb-10 pt-10 sm:px-6 sm:pb-14 sm:pt-14 lg:grid-cols-[1.1fr_1fr] lg:gap-12 lg:py-16">
        <div>
          <h1 className="text-balance font-hero text-[30px] font-bold leading-[1.1] tracking-tight sm:text-4xl lg:text-[44px]">
            {t.rich("title", {
              accent: (chunks) => <span className="text-accent-300">{chunks}</span>,
              br: () => <br />,
            })}
          </h1>
          <p className="mt-4 max-w-[46ch] text-pretty text-[15px] text-white/75 sm:text-base">{t("subtitle")}</p>

          <div className="mt-6 sm:mt-7">
            <HeroSearchCard />
          </div>

          {spots.length > 0 && (
            <p className="mt-4 text-sm text-white/70">
              {t("mapHint")}{" "}
              <Link href={MAP_PATH} className="font-semibold text-white underline-offset-4 hover:underline">
                {t("openMap")} →
              </Link>
            </p>
          )}
        </div>

        <div className="mx-auto w-full max-w-[300px] sm:max-w-[360px] lg:max-w-[420px]">
          <MoldovaMap spots={spots} ariaLabel={t("mapLabel")} />
        </div>
      </div>
    </section>
  );
}
