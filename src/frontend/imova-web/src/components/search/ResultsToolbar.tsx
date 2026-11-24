import type { ReactNode } from "react";
import { cn } from "@/lib/utils/cn";
import { SortSelect } from "./SortSelect";
import { ViewToggle } from "./ViewToggle";

// The row between the filters and the results: how many there are, and how they're shown — the
// list/map switch (on phones it sits in the filter bar instead) and sorting. sortOnPhones: false
// on the map, where there's no list to order on a phone. actions: extra buttons before those
// (the "save this search" button).
export function ResultsToolbar({
  summary,
  sortOnPhones = true,
  actions,
}: {
  summary: ReactNode;
  sortOnPhones?: boolean;
  actions?: ReactNode;
}) {
  return (
    // Wraps on a narrow phone (the count on its own line) instead of running off the screen —
    // "Salvează căutarea" and the sort menu don't both fit beside the count at 390px.
    <div className="mt-4 flex flex-wrap items-center justify-between gap-x-3 gap-y-2">
      <p className="text-sm text-ink-500" aria-live="polite">
        {summary}
      </p>
      <div className="ml-auto flex min-w-0 items-center gap-2">
        {actions}
        <div className="hidden lg:block">
          <ViewToggle />
        </div>
        <div className={cn("min-w-0", !sortOnPhones && "hidden lg:block")}>
          <SortSelect />
        </div>
      </div>
    </div>
  );
}
