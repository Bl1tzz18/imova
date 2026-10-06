import Link from "next/link";
import { pageWindow } from "@/lib/search/pagination";
import { cn } from "@/lib/utils/cn";

// Numbered pages as plain links — the same look as the search results' Pagination, for pages whose
// URLs aren't search URLs (the agency directory, an agency's listings).
export function LinkPagination({
  page,
  totalPages,
  hrefFor,
  labels,
}: {
  page: number;
  totalPages: number;
  hrefFor: (page: number) => string;
  labels: { nav: string; previous: string; next: string };
}) {
  if (totalPages <= 1) return null;

  const item = "flex h-10 min-w-10 items-center justify-center rounded-full px-3 text-sm font-medium transition-colors";

  return (
    <nav aria-label={labels.nav} className="mt-10 flex flex-wrap items-center justify-center gap-1">
      {page > 1 && (
        <Link href={hrefFor(page - 1)} rel="prev" className={cn(item, "text-ink-700 hover:bg-white")}>
          ← {labels.previous}
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
            href={hrefFor(p)}
            aria-current={p === page ? "page" : undefined}
            className={cn(item, p === page ? "bg-ink-950 text-white" : "text-ink-700 hover:bg-white")}
          >
            {p}
          </Link>
        ),
      )}
      {page < totalPages && (
        <Link href={hrefFor(page + 1)} rel="next" className={cn(item, "text-ink-700 hover:bg-white")}>
          {labels.next} →
        </Link>
      )}
    </nav>
  );
}
