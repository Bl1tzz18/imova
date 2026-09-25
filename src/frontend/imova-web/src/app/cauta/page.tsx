import type { Metadata } from "next";
import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { PropertyCard } from "@/components/property/PropertyCard";
import { FiltersSheet } from "@/components/search/FiltersSheet";
import { Pagination } from "@/components/search/Pagination";
import { SearchFilters } from "@/components/search/SearchFilters";
import { PendingResults, SearchNavigationProvider } from "@/components/search/SearchNavigation";
import { SortSelect } from "@/components/search/SortSelect";
import { searchListings, SEARCH_PAGE_SIZE } from "@/lib/search/api";
import { currentPage, parseSearchParams, singlePropertyType, type SearchState } from "@/lib/search/filters";

type PageProps = { searchParams: Promise<Record<string, string | string[] | undefined>> };

// "Apartamente de vânzare", "Anunțuri de închiriat", "Toate anunțurile", …
async function titleFor(state: SearchState): Promise<string> {
  const t = await getTranslations("Search");
  const type = singlePropertyType(state);
  const transaction = state.transactionType?.[0];
  const subject = type ? t(`typePlural.${type}`) : t("listings");
  if (transaction === "Sale") return t("titleSale", { subject });
  if (transaction === "Rent") return t("titleRent", { subject });
  return type ? subject : t("titleAll");
}

export async function generateMetadata({ searchParams }: PageProps): Promise<Metadata> {
  const state = parseSearchParams(await searchParams);
  const t = await getTranslations("Search");
  const title = await titleFor(state);
  return { title: `${title} — IMOVA`, description: t("metaDescription", { title: title.toLowerCase() }) };
}

// The single search results page: every entry point (hero, category tiles, Cumpără/Închiriază,
// the filter panel) just links here with query parameters, and this renders the matching listings
// on the server — so any /cauta URL can be bookmarked, shared or crawled and shows the same results.
export default async function SearchPage({ searchParams }: PageProps) {
  const state = parseSearchParams(await searchParams);
  const [t, tPage, results, title] = await Promise.all([
    getTranslations("Search"),
    getTranslations("SearchPage"),
    searchListings(state),
    titleFor(state),
  ]);
  const total = results?.totalCount ?? 0;
  const totalPages = Math.ceil(total / SEARCH_PAGE_SIZE);
  const page = currentPage(state);

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <SearchNavigationProvider state={state}>
          <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
            <div className="grid gap-8 lg:grid-cols-[290px_minmax(0,1fr)]">
              <aside className="hidden lg:block">
                <div className="sticky top-24 max-h-[calc(100vh-7rem)] overflow-y-auto rounded-[18px] border border-line bg-white p-5 shadow-[var(--shadow-card)]">
                  <SearchFilters />
                </div>
              </aside>

              <section className="min-w-0">
                <h1 className="font-hero text-2xl font-extrabold text-ink-950 sm:text-3xl">{title}</h1>
                <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
                  <p className="text-sm text-ink-500" aria-live="polite">
                    {results === null ? t("error") : tPage("resultsCount", { count: total })}
                  </p>
                  <div className="flex items-center gap-2">
                    <FiltersSheet totalCount={total} />
                    <SortSelect />
                  </div>
                </div>

                <PendingResults>
                  {results && results.items.length > 0 ? (
                    <>
                      <div className="mt-6 grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-3">
                        {results.items.map((listing) => (
                          <PropertyCard key={listing.id} listing={listing} />
                        ))}
                      </div>
                      <Pagination state={state} page={page} totalPages={totalPages} />
                    </>
                  ) : (
                    results && (
                      <div className="mt-6 flex flex-col items-center gap-3 rounded-[18px] border border-dashed border-line bg-white px-6 py-16 text-center">
                        <p className="font-semibold text-ink-900">{t("emptyTitle")}</p>
                        <p className="max-w-md text-sm text-ink-500">{t("emptyHint")}</p>
                        <Link href="/cauta" className="mt-2 text-sm font-medium text-accent-600 hover:underline">
                          {t("reset")}
                        </Link>
                      </div>
                    )
                  )}
                </PendingResults>
              </section>
            </div>
          </div>
        </SearchNavigationProvider>
      </main>
      <Footer />
    </div>
  );
}
