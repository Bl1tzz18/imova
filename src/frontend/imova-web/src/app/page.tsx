import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { Hero } from "@/components/home/Hero";
import { HomeBrowse } from "@/components/home/HomeBrowse";
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
        <Hero />

        <HomeBrowse listings={listings} addListingLabel={tCommon("addListing")} />

        {/* For owners: one invitation, with the reasons to list here beside it. */}
        <section className="mx-auto max-w-6xl px-4 pb-14 sm:px-6 sm:pb-20">
          <div className="grid gap-8 rounded-3xl bg-brand-900 p-6 sm:p-10 lg:grid-cols-[1fr_1.1fr] lg:items-center lg:gap-14 lg:p-12">
            <div>
              <h2 className="text-balance font-display text-2xl font-medium text-white sm:text-3xl">{t("ctaTitle")}</h2>
              <p className="mt-3 max-w-md text-sm text-brand-100 sm:text-base">{t("ctaSubtitle")}</p>
              <LinkButton href="/properties/new" size="lg" className="mt-6">
                {t("ctaButton")}
              </LinkButton>
            </div>
            <ul className="grid gap-5 sm:grid-cols-2">
              {valueProps.map((item) => (
                <li key={item.key} className="flex gap-3">
                  <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-white/10 text-brand-200">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" className="h-5 w-5" aria-hidden>
                      <path d={item.d} />
                    </svg>
                  </span>
                  <span>
                    <span className="block text-sm font-semibold text-white">{t(`valueProps.${item.key}.title`)}</span>
                    <span className="mt-0.5 block text-sm text-brand-100/80">{t(`valueProps.${item.key}.description`)}</span>
                  </span>
                </li>
              ))}
            </ul>
          </div>
        </section>
      </main>

      <Footer />
    </div>
  );
}
