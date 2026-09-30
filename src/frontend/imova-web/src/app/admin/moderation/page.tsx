import { redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { ModerationQueue } from "@/components/admin/ModerationQueue";
import { ReportedListingsQueue } from "@/components/admin/ReportedListingsQueue";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getSessionToken } from "@/lib/auth/session";
import Link from "next/link";
import { cn } from "@/lib/utils/cn";
import {
  MODERATION_PAGE_SIZE,
  MODERATION_TABS,
  REPORT_VIEWS,
  isListingTab,
  moderationHref,
  parseModerationTab,
  parsePage,
  parseReportView,
  statusForTab,
  type ListingTab,
  type ReportView,
} from "@/lib/admin/moderationTabs";
import { TextInput } from "@/components/ui/Field";
import { Button } from "@/components/ui/Button";
import type { Listing } from "@/types/listing";
import type { ListingReportSummary, ReportedListing } from "@/types/listingReport";

type PagedResult<T> = { items: T[]; page: number; pageSize: number; totalCount: number };

async function adminGet<T>(token: string, path: string): Promise<T> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}${path}`, { headers: { Authorization: `Bearer ${token}` }, cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Failed to fetch ${path}: ${res.status}`);
  }

  return res.json();
}

function getReportedListings(token: string, view: ReportView, page: number) {
  const params = new URLSearchParams({ resolved: String(view === "resolved"), page: String(page), pageSize: String(MODERATION_PAGE_SIZE) });
  return adminGet<PagedResult<ReportedListing>>(token, `/api/v1/admin/listing-reports?${params}`);
}

async function getModerationListings(token: string, tab: ListingTab, q: string, page: number): Promise<PagedResult<Listing>> {
  const params = new URLSearchParams({ status: statusForTab(tab), page: String(page), pageSize: String(MODERATION_PAGE_SIZE) });
  if (q) params.set("q", q);
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/admin/listings?${params}`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch moderation queue: ${res.status}`);
  }

  return res.json();
}

// ?tab=pending (the review queue, default) | active (suspend) | suspended (reinstate) | reports
// (what visitors reported; ?view=open|resolved); ?q= finds a listing by its id or link, title, or
// publisher name/email (not on the reports tab); ?page= pages through the results.
export default async function AdminModerationPage({
  searchParams,
}: {
  searchParams: Promise<{ tab?: string; q?: string; page?: string; view?: string }>;
}) {
  const params = await searchParams;
  const tab = parseModerationTab(params.tab);
  const view = parseReportView(params.view);
  const q = (params.q ?? "").trim().slice(0, 200);
  const page = parsePage(params.page);
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/moderation");
  }

  const [profile, t, tReports] = await Promise.all([
    getCurrentUserProfile(),
    getTranslations("AdminModerationPage"),
    getTranslations("AdminListingReports"),
  ]);

  if (!profile) {
    redirect("/login?next=/admin/moderation");
  }

  if (!profile.roles.includes("Admin")) {
    return (
      <div className="flex min-h-screen flex-col">
        <main className="flex-1">
          <div className="mx-auto max-w-2xl px-4 py-16 text-center sm:px-6">
            <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
              {t("forbiddenText")}
            </p>
          </div>
        </main>
        <Footer />
      </div>
    );
  }

  // The badge on the reports tab is on every tab, so waiting reports are noticed from anywhere.
  const [summary, listings, reported] = await Promise.all([
    adminGet<ListingReportSummary>(token, "/api/v1/admin/listing-reports/summary"),
    isListingTab(tab) ? getModerationListings(token, tab, q, page) : null,
    tab === "reports" ? getReportedListings(token, view, page) : null,
  ]);
  const totalCount = (listings ?? reported)!.totalCount;
  const pageCount = Math.max(1, Math.ceil(totalCount / MODERATION_PAGE_SIZE));

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-5xl px-4 py-10 sm:px-6">
          <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("title")}</h1>
          <p className="mt-1 text-sm text-ink-500">
            {tab === "reports"
              ? view === "open"
                ? tReports("openSummary", { listings: summary.openListings, reports: summary.openReports })
                : tReports("historySummary", { count: totalCount })
              : totalCount > 0
                ? t(`count.${tab}`, { count: totalCount })
                : q
                  ? t("noMatchTitle")
                  : t(`empty.${tab}`)}
          </p>

          <nav className="mt-6 flex gap-1 overflow-x-auto overflow-y-hidden border-b border-ink-100">
            {MODERATION_TABS.map((item) => (
              <Link
                key={item}
                href={moderationHref({ tab: item })}
                aria-current={item === tab ? "page" : undefined}
                className={cn(
                  "-mb-px whitespace-nowrap border-b-2 px-3.5 py-2.5 text-sm font-medium transition-colors",
                  item === tab ? "border-accent-500 text-ink-950" : "border-transparent text-ink-500 hover:text-ink-900",
                )}
              >
                {t(`tabs.${item}`)}
                {item === "reports" && summary.openListings > 0 && (
                  <span className="ml-1.5 inline-flex min-w-5 items-center justify-center rounded-full bg-red-600 px-1.5 text-[11px] font-semibold text-white">
                    {summary.openListings}
                  </span>
                )}
              </Link>
            ))}
          </nav>

          <div className="mt-8">
            {tab === "reports" && (
              <div className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div className="inline-flex rounded-full border border-ink-200 bg-white p-1 text-sm">
                  {REPORT_VIEWS.map((item) => (
                    <Link
                      key={item}
                      href={moderationHref({ tab, view: item })}
                      aria-current={item === view ? "page" : undefined}
                      className={cn(
                        "rounded-full px-3.5 py-1.5 font-medium transition-colors",
                        item === view ? "bg-ink-900 text-white" : "text-ink-600 hover:text-ink-900",
                      )}
                    >
                      {tReports(`views.${item}`)}
                      {item === "open" && summary.openListings > 0 && ` (${summary.openListings})`}
                    </Link>
                  ))}
                </div>
                <p className="text-xs text-ink-500">{view === "open" ? tReports("openHint") : tReports("historyHint")}</p>
              </div>
            )}

            {/* A plain GET form: the search lives in the URL, like the tab and the page. */}
            {tab !== "reports" && (
            <form action="/admin/moderation" className="mb-5 flex flex-col gap-2 sm:flex-row">
              {tab !== "pending" && <input type="hidden" name="tab" value={tab} />}
              <TextInput
                type="search"
                name="q"
                defaultValue={q}
                maxLength={200}
                placeholder={t("searchPlaceholder")}
                aria-label={t("searchLabel")}
                className="flex-1"
              />
              <div className="flex gap-2">
                <Button type="submit" variant="secondary">
                  {t("search")}
                </Button>
                {q && (
                  <Link
                    href={moderationHref({ tab })}
                    className="inline-flex h-11 items-center px-3 text-sm font-medium text-ink-500 hover:text-ink-900"
                  >
                    {t("clearSearch")}
                  </Link>
                )}
              </div>
            </form>
            )}

            {(listings ?? reported)!.items.length > 0 ? (
              <>
                {listings && isListingTab(tab) && <ModerationQueue key={`${tab}:${q}:${page}`} listings={listings.items} tab={tab} />}
                {reported && <ReportedListingsQueue key={`${view}:${page}`} cases={reported.items} view={view} now={new Date().toISOString()} />}
                {pageCount > 1 && (
                  <nav className="mt-6 flex items-center justify-between gap-3 text-sm" aria-label={t("pagination")}>
                    {page > 1 ? (
                      <Link href={moderationHref({ tab, q, view, page: page - 1 })} className="font-medium text-brand-700 hover:underline">
                        ← {t("previous")}
                      </Link>
                    ) : (
                      <span />
                    )}
                    <span className="text-ink-500">{t("pageOf", { page, pageCount })}</span>
                    {page < pageCount ? (
                      <Link href={moderationHref({ tab, q, view, page: page + 1 })} className="font-medium text-brand-700 hover:underline">
                        {t("next")} →
                      </Link>
                    ) : (
                      <span />
                    )}
                  </nav>
                )}
              </>
            ) : (
              <div className="flex flex-col items-center gap-2 rounded-2xl border border-dashed border-ink-200 bg-white px-6 py-16 text-center">
                {tab === "reports" ? (
                  <>
                    <p className="text-sm font-medium text-ink-700">{tReports(`empty.${view}`)}</p>
                    <p className="text-sm text-ink-500">{tReports(`emptyBody.${view}`)}</p>
                  </>
                ) : (
                  <>
                    <p className="text-sm font-medium text-ink-700">{q ? t("noMatchTitle") : t(`empty.${tab}`)}</p>
                    <p className="text-sm text-ink-500">{q ? t("noMatchBody", { q }) : t(`emptyBody.${tab}`)}</p>
                  </>
                )}
              </div>
            )}
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}
