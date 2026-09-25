import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { searchHref, updateSearch, type SearchState } from "@/lib/search/filters";
import { pageWindow } from "@/lib/search/pagination";
import { cn } from "@/lib/utils/cn";

// Numbered pages as plain links (?page=N) — crawlable, bookmarkable, and Back/Forward just work.
export async function Pagination({ state, page, totalPages }: { state: SearchState; page: number; totalPages: number }) {
  const t = await getTranslations("Search");
  if (totalPages <= 1) return null;

  const href = (p: number) => searchHref(updateSearch(state, { page: String(p) }));
  const item = "flex h-10 min-w-10 items-center justify-center rounded-full px-3 text-sm font-medium transition-colors";

  return (
    <nav aria-label={t("pagination")} className="mt-10 flex flex-wrap items-center justify-center gap-1">
      {page > 1 && (
        <Link href={href(page - 1)} rel="prev" className={cn(item, "text-ink-700 hover:bg-white")}>
          ← {t("previous")}
        </Link>
      )}
      {pageWindow(page, totalPages).map((p, i) =>
        p === null ? (
          <span key={`gap-${i}`} className="px-1 text-ink-400">
            …
          </span>
        ) : (
          <Link
            key={p}
            href={href(p)}
            aria-current={p === page ? "page" : undefined}
            className={cn(item, p === page ? "bg-ink-950 text-white" : "text-ink-700 hover:bg-white")}
          >
            {p}
          </Link>
        ),
      )}
      {page < totalPages && (
        <Link href={href(page + 1)} rel="next" className={cn(item, "text-ink-700 hover:bg-white")}>
          {t("next")} →
        </Link>
      )}
    </nav>
  );
}
