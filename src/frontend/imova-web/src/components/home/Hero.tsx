import { Link } from "@/i18n/navigation";
import { getTranslations } from "next-intl/server";
import { HeroSearchCard } from "@/components/home/HeroSearchCard";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { searchHref } from "@/lib/search/filters";

// One tap from the home page into a type's results.
const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;

// The hero photo: Sebastian Schuster, https://unsplash.com/photos/-y8fNw9x9wc — Unsplash License,
// free for commercial use with no credit required. Served as ready-made files, not through
// next/image: that resized the 2000 px original on the first request after every restart (seconds
// on the small server) and sent ~460 KB. These are cropped to the hero's shape — a wide band on a
// computer, nearly square-to-portrait on a phone — in AVIF/WebP, ~35–115 KB, cached for a year
// (next.config.js). To change the photo: replace public/images/hero-home.jpg, bump VERSION in
// scripts/make-hero-images.mjs, run it, and update HERO_PHOTO below.
const HERO_PHOTO = {
  dir: "/images/hero/home-v1",
  wide: [1280, 1600, 2000],
  tall: [480, 800],
};
const heroSrcSet = (crop: "wide" | "tall", format: "avif" | "webp") =>
  HERO_PHOTO[crop].map((w) => `${HERO_PHOTO.dir}-${crop}-${w}.${format} ${w}w`).join(", ");
const WIDE_MEDIA = "(min-width: 640px)";

// The home page's first screen, the way the big portals do it: one warm, lived-in photo, one line
// and one field ("where?"). The property types sit right under the photo as tiles that overlap its
// bottom edge (Airbnb/Booking's "browse by type"): on white, large enough for the icons to help —
// small icons on translucent chips over the photo read as noise. Sized to its content so the newest
// listings start soon after; on a phone the search lands in the first screen. Text sits on a soft
// shade over the photo — from the left on wide screens, from the bottom on phones.
export async function Hero() {
  const t = await getTranslations("Hero");
  const tSearch = await getTranslations("Search");
  const tile =
    "group flex flex-col items-center justify-center gap-2 rounded-xl border border-line bg-white px-2 py-3.5 text-center text-[13px] font-semibold leading-tight text-ink-900 shadow-card transition-[border-color,box-shadow] hover:border-accent-300 hover:shadow-card-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent-500 sm:py-4 sm:text-sm";
  const tileIcon =
    "h-7 w-7 text-accent-500 transition-transform group-hover:-translate-y-0.5 motion-reduce:transition-none sm:h-8 sm:w-8";

  return (
    <>
      {/* Layers: the photo and its shade at the bottom, the type tiles (overlapping the photo) above
          them, and the headline + search above the tiles — so the place suggestions drop over the
          tiles. The section itself must not form a stacking context (no isolate/z-index), or
          everything in it would sit under the tiles. */}
      <section className="relative flex min-h-[420px] items-end sm:items-center">
        {/* The wide crop from the sm breakpoint (where the shade turns to come from the left). */}
        <picture>
          <source media={WIDE_MEDIA} type="image/avif" srcSet={heroSrcSet("wide", "avif")} sizes="100vw" />
          <source media={WIDE_MEDIA} type="image/webp" srcSet={heroSrcSet("wide", "webp")} sizes="100vw" />
          <source type="image/avif" srcSet={heroSrcSet("tall", "avif")} sizes="100vw" />
          <img
            src={`${HERO_PHOTO.dir}-tall-800.webp`}
            srcSet={heroSrcSet("tall", "webp")}
            sizes="100vw"
            alt=""
            fetchPriority="high"
            decoding="async"
            className="absolute inset-0 z-0 h-full w-full object-cover object-[60%_30%]"
          />
        </picture>
        <div className="absolute inset-0 z-0 bg-gradient-to-t from-black/65 via-black/25 to-black/0 sm:bg-gradient-to-r sm:from-black/55 sm:via-black/25 sm:to-black/0" />

        {/* The bottom padding leaves room for the tiles that overlap the photo's lower edge. */}
        <div className="relative z-20 mx-auto w-full max-w-6xl px-4 pb-16 pt-24 sm:px-6 sm:pb-20 sm:pt-14">
          <h1 className="max-w-[580px] text-balance font-hero text-[30px] font-extrabold leading-[1.08] tracking-tight text-white drop-shadow-[0_2px_16px_rgba(0,0,0,0.35)] sm:text-[40px] lg:text-[44px]">
            {t("title")}
          </h1>
          {/* Phones have the same field in the header (HeaderSearch), so the photo keeps just the line. */}
          <div className="mt-6 hidden max-w-[580px] md:block">
            <HeroSearchCard />
          </div>
        </div>
      </section>

      {/* Six types: a 3×2 grid on a phone (both rows as tall as the taller one, since "Spații
          comerciale" wraps), one row from a tablet up. The map has its own section further down
          (HomeMapSection) — it's a way of looking, not a kind of property. */}
      <nav aria-label={t("quickLinks")} className="relative z-10 mx-auto -mt-9 w-full max-w-6xl px-4 sm:-mt-11 sm:px-6">
        <ul className="grid auto-rows-fr grid-cols-3 gap-2 sm:grid-cols-6 sm:gap-3">
          {PROPERTY_TYPES.map((type) => (
            <li key={type} className="grid">
              <Link href={searchHref({ propertyType: [type] })} className={tile}>
                <PropertyIcon type={type} className={tileIcon} />
                {tSearch(`typePlural.${type}`)}
              </Link>
            </li>
          ))}
        </ul>
      </nav>
    </>
  );
}
