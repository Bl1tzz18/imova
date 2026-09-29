import { redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { ModerationQueue } from "@/components/admin/ModerationQueue";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getSessionToken } from "@/lib/auth/session";
import Link from "next/link";
import { cn } from "@/lib/utils/cn";
import {
  MODERATION_PAGE_SIZE,
  MODERATION_TABS,
  moderationHref,
  parseModerationTab,
  parsePage,
  statusForTab,
  type ModerationTab,
} from "@/lib/admin/moderationTabs";
import { TextInput } from "@/components/ui/Field";
import { Button } from "@/components/ui/Button";
import type { Listing } from "@/types/listing";

type PagedResult<T> = { items: T[]; page: number; pageSize: number; totalCount: number };

async function getModerationListings(token: string, tab: ModerationTab, q: string, page: number): Promise<PagedResult<Listing>> {
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

// ?tab=pending (the review queue, default) | active (suspend) | suspended (reinstate); ?q= finds a
// listing by its id or link, title, or publisher name/email; ?page= pages through the results.
export default async function AdminModerationPage({
  searchParams,
}: {
  searchParams: Promise<{ tab?: string; q?: string; page?: string }>;
}) {
  const params = await searchParams;
  const tab = parseModerationTab(params.tab);
  const q = (params.q ?? "").trim().slice(0, 200);
  const page = parsePage(params.page);
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/moderation");
  }

  const [profile, t] = await Promise.all([
    getCurrentUserProfile(),
    getTranslations("AdminModerationPage"),
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

  const result = await getModerationListings(token, tab, q, page);
  const pageCount = Math.max(1, Math.ceil(result.totalCount / MODERATION_PAGE_SIZE));

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-5xl px-4 py-10 sm:px-6">
          <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("title")}</h1>
          <p className="mt-1 text-sm text-ink-500">
            {result.totalCount > 0 ? t(`count.${tab}`, { count: result.totalCount }) : q ? t("noMatchTitle") : t(`empty.${tab}`)}
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
              </Link>
            ))}
          </nav>

          <div className="mt-8">
            {/* A plain GET form: the search lives in the URL, like the tab and the page. */}
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

            {result.items.length > 0 ? (
              <>
                <ModerationQueue key={`${tab}:${q}:${page}`} listings={result.items} tab={tab} />
                {pageCount > 1 && (
                  <nav className="mt-6 flex items-center justify-between gap-3 text-sm" aria-label={t("pagination")}>
                    {page > 1 ? (
                      <Link href={moderationHref({ tab, q, page: page - 1 })} className="font-medium text-brand-700 hover:underline">
                        ← {t("previous")}
                      </Link>
                    ) : (
                      <span />
                    )}
                    <span className="text-ink-500">{t("pageOf", { page, pageCount })}</span>
                    {page < pageCount ? (
                      <Link href={moderationHref({ tab, q, page: page + 1 })} className="font-medium text-brand-700 hover:underline">
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
                <p className="text-sm font-medium text-ink-700">{q ? t("noMatchTitle") : t(`empty.${tab}`)}</p>
                <p className="text-sm text-ink-500">{q ? t("noMatchBody", { q }) : t(`emptyBody.${tab}`)}</p>
              </div>
            )}
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}
