import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { EmailConfirmationBanner } from "@/components/auth/EmailConfirmationBanner";
import { Footer } from "@/components/layout/Footer";
import { SavedSearchList, type SavedSearchRow } from "@/components/savedSearches/SavedSearchList";
import { LinkButton } from "@/components/ui/Button";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getSavedSearches } from "@/lib/savedSearches/api";
import { SAVED_SEARCHES_PATH } from "@/lib/savedSearches/savedSearch";
import { activeFilterCount, parseSearchParams, SEARCH_PATH } from "@/lib/search/filters";
import { searchTitle } from "@/lib/search/pageTitle";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("SavedSearches");
  return { title: `${t("title")} — IMOVA` };
}

export default async function SavedSearchesPage() {
  const [savedSearches, profile, t] = await Promise.all([
    getSavedSearches(),
    getCurrentUserProfile(),
    getTranslations("SavedSearches"),
  ]);
  if (savedSearches === null || !profile) {
    redirect(`/login?next=${SAVED_SEARCHES_PATH}`);
  }

  // Each search described the way its results page is titled, plus how many filters it has.
  const rows: SavedSearchRow[] = await Promise.all(
    savedSearches.map(async (s) => {
      const state = parseSearchParams(new URLSearchParams(s.queryString));
      const filters = activeFilterCount(state);
      const title = await searchTitle(state);
      return { ...s, summary: filters > 0 ? `${title} · ${t("filterCount", { count: filters })}` : title };
    }),
  );

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-4xl px-4 py-10 sm:px-6">
          <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("title")}</h1>
          <p className="mt-1 text-sm text-ink-500">{t("subtitle")}</p>

          {/* Alerts only go to confirmed addresses (SavedSearchAlerts). */}
          {!profile.emailConfirmed && rows.length > 0 && (
            <div className="mt-6">
              <p className="mb-2 text-sm text-ink-600">{t("alertsNeedConfirmedEmail")}</p>
              <EmailConfirmationBanner email={profile.email} />
            </div>
          )}

          <div className="mt-8">
            {rows.length > 0 ? (
              <SavedSearchList rows={rows} />
            ) : (
              <div className="flex flex-col items-center gap-3 rounded-[18px] border border-dashed border-line bg-white px-6 py-16 text-center">
                <p className="font-semibold text-ink-900">{t("emptyTitle")}</p>
                <p className="max-w-md text-sm text-ink-500">{t("emptyBody")}</p>
                <LinkButton href={SEARCH_PATH} className="mt-2">
                  {t("startSearching")}
                </LinkButton>
              </div>
            )}
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}
