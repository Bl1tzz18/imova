"use client";

import type { ReactNode } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { switchViewHref, type SearchView } from "@/lib/search/filters";
import { cn } from "@/lib/utils/cn";
import { useSearchNavigation } from "./SearchNavigation";

const VIEW_ICONS: Record<SearchView, ReactNode> = {
  list: <path d="M8 6h13M8 12h13M8 18h13M3.5 6h.01M3.5 12h.01M3.5 18h.01" />,
  map: <path d="M9 4 3 6.5v13.5L9 17.5l6 2.5 6-2.5V4l-6 2.5L9 4ZM9 4v13.5M15 6.5V20" />,
};

// Listă | Hartă — the same search as cards or as pins; switching keeps every filter.
export function ViewToggle() {
  const t = useTranslations("Search");
  const { state, view } = useSearchNavigation();
  return (
    <nav aria-label={t("viewLabel")} className="flex shrink-0 gap-1 rounded-full border border-line bg-white p-1">
      {(["list", "map"] as const).map((option) => (
        <Link
          key={option}
          href={switchViewHref(state, option)}
          aria-current={view === option ? "page" : undefined}
          className={cn(
            "flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-medium transition-colors",
            view === option ? "bg-ink-950 text-white" : "text-ink-600 hover:text-ink-950",
          )}
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4" aria-hidden>
            {VIEW_ICONS[option]}
          </svg>
          <span className="hidden sm:inline">{t(option === "list" ? "listView" : "mapView")}</span>
        </Link>
      ))}
    </nav>
  );
}

