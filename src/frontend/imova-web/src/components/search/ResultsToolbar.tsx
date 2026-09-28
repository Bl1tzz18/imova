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
    <div className="mt-4 flex items-center justify-between gap-3">
      <p className="text-sm text-ink-500" aria-live="polite">
        {summary}
      </p>
      <div className="flex shrink-0 items-center gap-2">
        {actions}
        <div className="hidden lg:block">
          <ViewToggle />
        </div>
        <div className={cn(!sortOnPhones && "hidden lg:block")}>
          <SortSelect />
        </div>
      </div>
    </div>
  );
}
