import type { Metadata } from "next";
import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { PropertyCard } from "@/components/property/PropertyCard";
import { AppliedFilters } from "@/components/search/AppliedFilters";
import { FilterOptionsProvider } from "@/components/search/FilterOptions";
import { Pagination } from "@/components/search/Pagination";
import { SearchFilterBar } from "@/components/search/SearchFilterBar";
import { SortSelect } from "@/components/search/SortSelect";
import { PendingResults, SearchNavigationProvider } from "@/components/search/SearchNavigation";
import { searchListings, SEARCH_PAGE_SIZE } from "@/lib/search/api";
import { currentPage, parseSearchParams } from "@/lib/search/filters";
import { searchTitle } from "@/lib/search/pageTitle";

type PageProps = { searchParams: Promise<Record<string, string | string[] | undefined>> };

export async function generateMetadata({ searchParams }: PageProps): Promise<Metadata> {
  const state = parseSearchParams(await searchParams);
  const t = await getTranslations("Search");
  const title = await searchTitle(state);
  return { title: `${title} — IMOVA`, description: t("metaDescription", { title: title.toLowerCase() }) };
}

// The single search results page: every entry point (hero, category tiles, Cumpără/Închiriază,
// the filter panel) just links here with query parameters, and this renders the matching listings
// on the server — so any /search URL can be bookmarked, shared or crawled and shows the same results.
export default async function SearchPage({ searchParams }: PageProps) {
  const state = parseSearchParams(await searchParams);
  const [t, tPage, results, title] = await Promise.all([
    getTranslations("Search"),
    getTranslations("SearchPage"),
    searchListings(state),
    searchTitle(state),
  ]);
  const total = results?.totalCount ?? 0;
  const totalPages = Math.ceil(total / SEARCH_PAGE_SIZE);
  const page = currentPage(state);

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <SearchNavigationProvider state={state}>
          <FilterOptionsProvider>
            <div className="mx-auto max-w-[1440px] px-4 sm:px-6">
              <h1 className="pt-8 font-hero text-2xl font-extrabold text-ink-950 sm:text-3xl">{title}</h1>

              {/* Stays under the site header while scrolling through results. */}
              <div className="sticky top-20 z-20 -mx-4 mt-4 border-b border-line bg-ink-50/95 px-4 py-3 backdrop-blur sm:-mx-6 sm:px-6">
                <SearchFilterBar totalCount={total} />
              </div>

              <div className="pb-8">
                <AppliedFilters />
                <div className="mt-4 flex items-center justify-between gap-3">
                  <p className="text-sm text-ink-500" aria-live="polite">
                    {results === null ? t("error") : tPage("resultsCount", { count: total })}
                  </p>
                  {/* Phones: sorting lives here — the sticky bar keeps it on wider screens. */}
                  <div className="lg:hidden">
                    <SortSelect />
                  </div>
                </div>

                <PendingResults>
                  {results && results.items.length > 0 ? (
                    <>
                      <div className="mt-4 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                        {results.items.map((listing) => (
                          <PropertyCard key={listing.id} listing={listing} />
                        ))}
                      </div>
                      <Pagination state={state} page={page} totalPages={totalPages} />
                    </>
                  ) : (
                    results && (
                      <div className="mt-4 flex flex-col items-center gap-3 rounded-[18px] border border-dashed border-line bg-white px-6 py-16 text-center">
                        <p className="font-semibold text-ink-900">{t("emptyTitle")}</p>
                        <p className="max-w-md text-sm text-ink-500">{t("emptyHint")}</p>
                        <Link href="/search" className="mt-2 text-sm font-medium text-accent-600 hover:underline">
                          {t("reset")}
                        </Link>
                      </div>
                    )
                  )}
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
