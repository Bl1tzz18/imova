import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { AutoSubmitForm } from "@/components/agency/AutoSubmitForm";
import { LinkPagination } from "@/components/agency/LinkPagination";
import { OwnerListingRow } from "@/components/property/OwnerListingsList";
import { inputClass } from "@/components/ui/Field";
import { agencyListingsTabHref, agencyManagePath, tabOfGroup, type AgencyListingsTabState } from "@/lib/agency/manage";
import { OWNER_GROUPS, OWNER_SORTS, OWNER_TOOLS_FROM } from "@/lib/listing/ownerGroups";
import { cn } from "@/lib/utils/cn";
import type { AgencyListingsPage } from "@/types/agency";

// The Anunțuri tab: "Anunțurile mele"'s tabs, search, sort, rows and actions, but paged by the API —
// an agency can have hundreds of listings. Everything is in the address (?tab=&q=&sort=&page=): the
// tabs are links, search and sort a GET form (Enter in the search, or changing the sort, sends it),
// pages the numbered links of the public pages. Counts and the attention dot come from the API,
// after the search.
export async function AgencyListingsList({
  agencyId,
  state,
  result,
  showAuthor,
}: {
  agencyId: string;
  state: AgencyListingsTabState;
  result: AgencyListingsPage;
  showAuthor: boolean;
}) {
  const [t, tAgency, tSearch] = await Promise.all([
    getTranslations("MyListingsPage"),
    getTranslations("AgencyManage"),
    getTranslations("Search"),
  ]);
  const now = new Date();
  const tab = tabOfGroup(result.group);
  const summaries = { active: result.active, unpublished: result.unpublished, ended: result.ended };
  const total = result.active.count + result.unpublished.count + result.ended.count;
  const showTools = state.q !== "" || total >= OWNER_TOOLS_FROM;
  const totalPages = Math.max(1, Math.ceil(result.totalCount / result.pageSize));

  return (
    <div>
      {showTools && (
        <AutoSubmitForm action={agencyManagePath(agencyId, "listings")} className="mb-3 flex gap-2">
          <input type="hidden" name="tab" value={tab} />
          <label className="relative min-w-0 flex-1">
            <span className="sr-only">{t("searchLabel")}</span>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400" aria-hidden>
              <circle cx="11" cy="11" r="6.5" />
              <path d="m20 20-4.2-4.2" strokeLinecap="round" />
            </svg>
            <input type="search" name="q" defaultValue={state.q} placeholder={t("searchPlaceholder")} className={cn(inputClass, "pl-10")} />
          </label>
          {/* As in "Anunțurile mele": a full select on wider screens, an icon over an invisible native
              select on a phone. */}
          <label className="relative flex h-11 w-11 shrink-0 items-center justify-center rounded-xl border border-ink-200 bg-white text-ink-700 focus-within:border-brand-500 focus-within:ring-2 focus-within:ring-brand-500/20 sm:w-auto sm:border-0 sm:bg-transparent sm:focus-within:ring-0">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-5 w-5 sm:hidden" aria-hidden>
              <path d="M7 4v16M4 17l3 3 3-3M17 20V4M14 7l3-3 3 3" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            <select
              name="sort"
              aria-label={t("sortLabel")}
              defaultValue={state.sort}
              className={cn(inputClass, "absolute inset-0 h-full w-full opacity-0 sm:static sm:w-auto sm:pr-8 sm:opacity-100")}
            >
              {OWNER_SORTS.map((s) => (
                <option key={s} value={s}>
                  {t(`sort.${s}`)}
                </option>
              ))}
            </select>
          </label>
        </AutoSubmitForm>
      )}

      <nav aria-label={t("title")} className="grid grid-cols-3 gap-1 rounded-2xl bg-ink-100/70 p-1 sm:max-w-xl">
        {OWNER_GROUPS.map((group) => {
          const selected = tab === group;
          return (
            <Link
              key={group}
              href={agencyListingsTabHref(agencyId, { tab: group, q: state.q, sort: state.sort })}
              aria-current={selected ? "page" : undefined}
              className={cn(
                "relative flex min-h-11 flex-col items-center justify-center gap-0.5 rounded-xl px-2 py-1.5 text-center text-sm font-medium leading-tight transition-colors sm:flex-row sm:gap-2",
                selected ? "bg-white text-ink-950 shadow-sm" : "text-ink-600 hover:text-ink-900",
              )}
            >
              <span>{t(`group.${group}`)}</span>
              <span className={cn("text-xs tabular-nums", selected ? "text-ink-500" : "text-ink-400")}>{summaries[group].count}</span>
              {summaries[group].needsAttention && (
                <span className="absolute right-2 top-2 h-2 w-2 rounded-full bg-accent-500" title={t("needsAttention")}>
                  <span className="sr-only">{t("needsAttention")}</span>
                </span>
              )}
            </Link>
          );
        })}
      </nav>

      {result.items.length > 0 ? (
        <>
          <ul className="mt-5 flex flex-col gap-3">
            {result.items.map((listing) => (
              <OwnerListingRow key={listing.id} listing={listing} now={now} showStatus={tab !== "active"} showAuthor={showAuthor} />
            ))}
          </ul>
          <LinkPagination
            page={result.page}
            totalPages={totalPages}
            hrefFor={(page) => agencyListingsTabHref(agencyId, { tab, q: state.q, sort: state.sort, page })}
            labels={{ nav: tSearch("pagination"), previous: tSearch("previous"), next: tSearch("next") }}
          />
        </>
      ) : (
        <div className="mt-6 flex flex-col items-center gap-2 rounded-2xl border border-dashed border-ink-200 bg-white px-6 py-14 text-center">
          <p className="text-sm font-medium text-ink-700">
            {total === 0 && !state.q
              ? tAgency("listingsEmptyTitle")
              : state.q
                ? t("searchEmpty", { query: state.q })
                : t(`groupEmpty.${tab}`)}
          </p>
          {total === 0 && !state.q && <p className="text-sm text-ink-500">{tAgency("listingsEmptyBody")}</p>}
          {state.q && (
            <Link href={agencyListingsTabHref(agencyId, { tab, sort: state.sort })} className="text-sm font-medium text-brand-700 hover:underline">
              {t("clearSearch")}
            </Link>
          )}
        </div>
      )}
    </div>
  );
}
