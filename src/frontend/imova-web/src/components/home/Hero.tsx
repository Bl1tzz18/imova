import Image from "next/image";
import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { HeroSearchCard } from "@/components/home/HeroSearchCard";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { MAP_PATH, searchHref } from "@/lib/search/filters";

// One tap from the hero into a type's results — the quick filters the big portals put under their
// search box.
const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;

// The hero photo (Unsplash License: free for commercial use; we credit the photographer anyway).
// Swap the file in public/images and these two lines to change it.
const HERO_PHOTO = {
  src: "/images/hero-home.jpg",
  author: "Sebastian Schuster",
  url: "https://unsplash.com/photos/-y8fNw9x9wc",
};

// The home page's first screen, the way the big portals do it: one warm, lived-in photo, one line,
// one field ("where?"), and the property types as quick chips under it. Sized to its content
// (about 440 px on a desktop) so the newest listings start within the first screen; on a phone the
// search lands in the first screen. Text sits on a soft shade over the photo — from the left on
// wide screens, from the bottom on phones.
export async function Hero() {
  const t = await getTranslations("Hero");
  const tSearch = await getTranslations("Search");
  const chip =
    "flex shrink-0 items-center gap-1.5 whitespace-nowrap rounded-full border border-white/25 bg-black/25 px-3 py-1.5 text-[13px] font-medium text-white backdrop-blur-sm transition-colors hover:border-white/60 hover:bg-black/40";

  return (
    <section className="relative isolate flex min-h-[440px] items-end sm:items-center lg:min-h-[460px]">
      <Image
        src={HERO_PHOTO.src}
        alt=""
        fill
        priority
        sizes="100vw"
        className="-z-20 object-cover object-[60%_30%]"
      />
      <div className="absolute inset-0 -z-10 bg-gradient-to-t from-black/65 via-black/25 to-black/0 sm:bg-gradient-to-r sm:from-black/55 sm:via-black/25 sm:to-black/0" />

      <div className="mx-auto w-full max-w-6xl px-4 pb-8 pt-24 sm:px-6 sm:py-14">
        <div>
          <h1 className="max-w-[580px] text-balance font-hero text-[30px] font-extrabold leading-[1.08] tracking-tight text-white drop-shadow-[0_2px_16px_rgba(0,0,0,0.35)] sm:text-[40px] lg:text-[44px]">
            {t("title")}
          </h1>
          <div className="mt-5 max-w-[580px] sm:mt-6">
            <HeroSearchCard />
          </div>
          <nav aria-label={t("quickLinks")} className="-mx-4 mt-4 overflow-x-auto px-4 [scrollbar-width:none] sm:mx-0 sm:px-0 [&::-webkit-scrollbar]:hidden">
            <ul className="flex gap-2 sm:flex-wrap">
              {PROPERTY_TYPES.map((type) => (
                <li key={type}>
                  <Link href={searchHref({ propertyType: [type] })} className={chip}>
                    <PropertyIcon type={type} className="h-4 w-4" />
                    {tSearch(`typePlural.${type}`)}
                  </Link>
                </li>
              ))}
              <li>
                <Link href={MAP_PATH} className={chip}>
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4" aria-hidden>
                    <path d="M9 4 3 6v14l6-2 6 2 6-2V4l-6 2-6-2ZM9 4v14M15 6v14" />
                  </svg>
                  {t("mapLink")}
                </Link>
              </li>
            </ul>
          </nav>
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
