import Image from "next/image";
import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { HeroSearchCard } from "@/components/home/HeroSearchCard";
import { MAP_PATH } from "@/lib/search/filters";

// The hero photo (Unsplash License: free for commercial use; we credit the photographer anyway).
// Swap the file in public/images and these two lines to change it.
const HERO_PHOTO = {
  src: "/images/hero-home.jpg",
  author: "Sebastian Schuster",
  url: "https://unsplash.com/photos/-y8fNw9x9wc",
};

// The home page's first screen, the way the big portals do it: one warm, lived-in photo, one line,
// one field ("where?"). Buying, selling and renting get their own cards right below (HomeIntents),
// so the hero doesn't have to ask. Text sits on a soft shade over the photo — from the left on wide
// screens, from the bottom on phones, where the search lands in the first screen.
export async function Hero() {
  const t = await getTranslations("Hero");

  return (
    <section className="relative isolate flex min-h-[480px] items-end sm:min-h-[540px] sm:items-center lg:min-h-[580px]">
      <Image
        src={HERO_PHOTO.src}
        alt=""
        fill
        priority
        sizes="100vw"
        className="-z-20 object-cover object-[60%_30%]"
      />
      <div className="absolute inset-0 -z-10 bg-gradient-to-t from-black/60 via-black/20 to-black/0 sm:bg-gradient-to-r sm:from-black/50 sm:via-black/20 sm:to-black/0" />

      <div className="mx-auto w-full max-w-6xl px-4 pb-10 pt-28 sm:px-6 sm:py-16">
        <div className="max-w-[640px]">
          <h1 className="text-balance font-hero text-[34px] font-extrabold leading-[1.06] tracking-tight text-white drop-shadow-[0_2px_16px_rgba(0,0,0,0.35)] sm:text-5xl lg:text-[56px]">
            {t("title")}
          </h1>
          <div className="mt-6 sm:mt-8">
            <HeroSearchCard />
          </div>
          <p className="mt-4 text-sm">
            <Link href={MAP_PATH} className="font-semibold text-white drop-shadow-[0_1px_4px_rgba(0,0,0,0.5)] underline-offset-4 hover:underline">
              {t("mapLink")} →
            </Link>
          </p>
        </div>
      </div>

      <a
        href={HERO_PHOTO.url}
        target="_blank"
        rel="noopener noreferrer"
        className="absolute bottom-2 right-3 text-[11px] text-white/80 drop-shadow-[0_1px_3px_rgba(0,0,0,0.6)] hover:underline"
      >
        {t("photoCredit", { author: HERO_PHOTO.author })}
      </a>
    </section>
  );
}
