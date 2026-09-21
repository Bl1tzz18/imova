import Image from "next/image";
import { getTranslations } from "next-intl/server";
import { HeroSearchCard } from "@/components/home/HeroSearchCard";

// Single reference point for the hero background image — swap this path (and the file at
// public/images/hero-bg.jpeg) to change it without touching the component below.
const HERO_IMAGE_SRC = "/images/hero-bg.jpg";

const STATS = ["houses", "apartments", "commercial", "land"] as const;

// Placeholder counts matching the current design mock — not live data. Real per-type counts
// already exist elsewhere on the homepage via PropertyTypeStats if that's ever wired in here
// instead.
const STAT_VALUES: Record<(typeof STATS)[number], number> = {
  houses: 295,
  apartments: 533,
  commercial: 120,
  land: 187,
};

export async function Hero() {
  const t = await getTranslations("Hero");

  return (
    <section className="relative flex min-h-[560px] items-center overflow-hidden py-10 sm:min-h-[600px] lg:min-h-[660px]">
      <Image src={HERO_IMAGE_SRC} alt="" fill priority sizes="100vw" className="object-cover" />
      {/* Transparent at the top, deepening to near-black at the bottom, so white text stays
          legible over any part of the underlying photo. */}
      <div className="absolute inset-0 bg-gradient-to-b from-transparent via-black/55 to-black/90" />

      <div className="relative z-10 mx-auto flex w-full max-w-4xl flex-col items-center px-4 py-16 text-center sm:px-6">
        <span className="mb-4 inline-flex items-center rounded-full border border-white/20 bg-white/10 px-3 py-1 text-[11px] font-semibold text-white backdrop-blur-sm">
          {t("eyebrow")}
        </span>
        <h1 className="text-balance font-hero text-2xl font-bold leading-[1.2] text-white sm:text-3xl lg:text-4xl">
          {t("title")}
        </h1>
        <p className="mt-3 max-w-lg text-balance text-sm text-white/80 sm:text-base">{t("subtitle")}</p>

        <div className="mt-7 grid w-full max-w-xs grid-cols-1 gap-2.5 sm:max-w-none sm:flex sm:flex-wrap sm:justify-center sm:gap-3">
          {STATS.map((key) => (
            <div
              key={key}
              className="flex flex-col items-center gap-0.5 rounded-xl border border-white/15 bg-white/10 px-4 py-2.5 backdrop-blur-sm sm:min-w-[110px]"
            >
              <span className="font-hero text-xl font-bold text-brand-200 sm:text-2xl">{STAT_VALUES[key]}</span>
              <span className="text-[11px] font-medium text-white/75">{t(`stats.${key}`)}</span>
            </div>
          ))}
        </div>

        <div className="mt-7 w-full">
          <HeroSearchCard />
        </div>
      </div>
    </section>
  );
}
