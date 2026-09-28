import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { PropertyMapExplorer } from "@/components/property/PropertyMapExplorer";
import { AppliedFilters } from "@/components/search/AppliedFilters";
import { ResultsToolbar } from "@/components/search/ResultsToolbar";
import { FilterOptionsProvider } from "@/components/search/FilterOptions";
import { SearchFilterBar } from "@/components/search/SearchFilterBar";
import { PendingResults, SearchNavigationProvider } from "@/components/search/SearchNavigation";
import { searchMapListings } from "@/lib/search/api";
import { parseSearchParams, searchHref, switchViewHref } from "@/lib/search/filters";
import { searchTitle } from "@/lib/search/pageTitle";
import { SaveSearchButton } from "@/components/savedSearches/SaveSearchButton";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { queryToSave } from "@/lib/savedSearches/savedSearch";

type PageProps = { searchParams: Promise<Record<string, string | string[] | undefined>> };

export async function generateMetadata({ searchParams }: PageProps): Promise<Metadata> {
  const state = parseSearchParams(await searchParams);
  const t = await getTranslations("Search");
  const title = await searchTitle(state);
  return { title: `${title} · ${t("mapView")} — IMOVA`, description: t("metaDescription", { title: title.toLowerCase() }) };
}

// The map view of a search: the same URL filters as /search (the "Listă | Hartă" switch carries
// them over both ways) and the same filter bar, with every matching listing that has coordinates
// as a pin instead of pages of cards.
export default async function MapPage({ searchParams }: PageProps) {
  const state = parseSearchParams(await searchParams);
  const [t, tMap, results, title, profile] = await Promise.all([
    getTranslations("Search"),
    getTranslations("MapPage"),
    searchMapListings(state),
    searchTitle(state),
    getCurrentUserProfile(),
  ]);

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <SearchNavigationProvider state={state} view="map">
          <FilterOptionsProvider>
            <div className="mx-auto max-w-[1440px] px-4 sm:px-6">
              <h1 className="pt-8 font-hero text-2xl font-extrabold text-ink-950 sm:text-3xl">{title}</h1>

              <div className="sticky top-20 z-20 -mx-4 mt-4 border-b border-line bg-ink-50/95 px-4 py-3 backdrop-blur sm:-mx-6 sm:px-6">
                <SearchFilterBar totalCount={results?.totalCount ?? 0} />
              </div>

              <div className="pb-8">
                <AppliedFilters />
                <ResultsToolbar
                  summary={results === null ? t("error") : tMap("resultsCount", { count: results.totalCount })}
                  sortOnPhones={false}
                  actions={
                    <SaveSearchButton
                      queryString={queryToSave(state)}
                      defaultName={title}
                      signedIn={profile !== null}
                      returnTo={searchHref(state, "map")}
                    />
                  }
                />
                <PendingResults>
                  <div className="mt-4">
                    {results !== null && (
                      <PropertyMapExplorer listings={results.items} totalCount={results.totalCount} listHref={switchViewHref(state, "list")} />
                    )}
                  </div>
                </PendingResults>
              </div>
            </div>
          </FilterOptionsProvider>
        </SearchNavigationProvider>
      </main>
      <Footer />
    </div>
  );
}
