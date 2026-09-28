import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { Hero } from "@/components/home/Hero";
import { PropertyCarousel } from "@/components/property/PropertyCarousel";
import { PropertyMapPromo } from "@/components/property/PropertyMapPromo";
import { PropertyTypeStats } from "@/components/property/PropertyTypeStats";
import { LinkButton } from "@/components/ui/Button";
import { getSessionToken } from "@/lib/auth/session";
import type { Listing } from "@/types/listing";

async function getListings(): Promise<Listing[]> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  const res = await fetch(`${apiUrl}/api/v1/listings`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch listings: ${res.status}`);
  }

  return res.json();
}

export default async function Home() {
  const [listings, t, tCommon] = await Promise.all([
    getListings(),
    getTranslations("Home"),
    getTranslations("Common"),
  ]);

  const valueProps = [
    { key: "noCommission", d: "M12 3v18M17 7.5c0-1.7-2.2-3-5-3s-5 1.3-5 3 2.2 3 5 3 5 1.3 5 3-2.2 3-5 3-5-1.3-5-3" },
    {
      key: "directSource",
      d: "M17 21v-2a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v2M10 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75",
    },
    { key: "quickPublish", d: "M12 8v4l3 3M12 3a9 9 0 1 0 9 9" },
    {
      key: "nationalCoverage",
      d: "M12 21s-7-6.1-7-11a7 7 0 1 1 14 0c0 4.9-7 11-7 11Z M12 10.5a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5Z",
    },
  ] as const;

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <Hero
          countsByType={listings.reduce<Record<string, number>>((counts, l) => {
            counts[l.property.propertyType] = (counts[l.property.propertyType] ?? 0) + 1;
            return counts;
          }, {})}
        />

        <section className="mx-auto max-w-6xl px-4 pt-12 sm:px-6">
          <h2 className="mb-5 font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("browseByType")}</h2>
          <PropertyTypeStats listings={listings} />
        </section>

        <section className="border-b border-ink-100 bg-white">
          <div className="mx-auto grid max-w-6xl grid-cols-2 gap-8 px-4 pb-10 pt-14 sm:px-6 sm:pt-16 md:grid-cols-4">
            {valueProps.map((item) => (
              <div key={item.key} className="flex flex-col items-start gap-2">
                <span className="flex h-10 w-10 items-center justify-center rounded-full bg-brand-50 text-brand-700">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" className="h-5 w-5">
                    <path d={item.d} />
                  </svg>
                </span>
                <p className="text-sm font-semibold text-ink-900">
                  {t(`valueProps.${item.key}.title`)}
                </p>
                <p className="text-sm text-ink-500">
                  {t(`valueProps.${item.key}.description`)}
                </p>
              </div>
            ))}
          </div>
        </section>

        <section className="mx-auto max-w-6xl px-4 pt-14 sm:px-6">
          <PropertyMapPromo listings={listings} />
        </section>

        <section className="mx-auto max-w-6xl px-4 py-14 sm:px-6">
          <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
            <div>
              <h2 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">
                {t("recentListings")}
              </h2>
              <p className="mt-1 text-sm text-ink-500">
                {listings.length > 0
                  ? t("listingsAvailable", { count: listings.length })
                  : t("noListingsYet")}
              </p>
            </div>
            <LinkButton href="/search" variant="secondary" size="sm">
              {t("viewAll")}
            </LinkButton>
          </div>

          {listings.length > 0 ? (
            <PropertyCarousel listings={listings} />
          ) : (
            <div className="flex flex-col items-center gap-4 rounded-2xl border border-dashed border-ink-200 bg-white px-6 py-16 text-center">
              <p className="text-sm text-ink-500">{t("emptyStateText")}</p>
              <LinkButton href="/properties/new" size="sm">
                {tCommon("addListing")}
              </LinkButton>
            </div>
          )}
        </section>

        <section className="mx-auto max-w-6xl px-4 pb-16 sm:px-6">
          <div className="flex flex-col items-center gap-4 rounded-3xl bg-brand-900 px-6 py-14 text-center sm:px-16">
            <h2 className="font-display text-2xl font-medium text-white sm:text-3xl">
              {t("ctaTitle")}
            </h2>
            <p className="max-w-md text-sm text-brand-100 sm:text-base">
              {t("ctaSubtitle")}
            </p>
            <LinkButton href="/properties/new" size="lg" className="mt-2">
              {t("ctaButton")}
            </LinkButton>
          </div>
        </section>
      </main>

      <Footer />
    </div>
  );
}
