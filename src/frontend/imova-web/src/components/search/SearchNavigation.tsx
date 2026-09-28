"use client";

import { createContext, useContext, useTransition, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { searchHref, updateSearch, type SearchState, type SearchView } from "@/lib/search/filters";
import { cn } from "@/lib/utils/cn";

type SearchNavigationValue = {
  state: SearchState;
  // Which page the search is shown on — filter changes stay on it.
  view: SearchView;
  pending: boolean;
  // Apply a filter change: a new URL (a history entry, so Back undoes it) without a full reload —
  // the server re-renders the results for it.
  change: (changes: Record<string, string | string[] | null>) => void;
};

const SearchNavigationContext = createContext<SearchNavigationValue | null>(null);

export function SearchNavigationProvider({ state, view = "list", children }: { state: SearchState; view?: SearchView; children: ReactNode }) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();

  function change(changes: Record<string, string | string[] | null>) {
    const href = searchHref(updateSearch(state, changes), view);
    startTransition(() => router.push(href, { scroll: false }));
  }

  return <SearchNavigationContext.Provider value={{ state, view, pending, change }}>{children}</SearchNavigationContext.Provider>;
}

export function useSearchNavigation(): SearchNavigationValue {
  const value = useContext(SearchNavigationContext);
  if (!value) throw new Error("useSearchNavigation must be used inside SearchNavigationProvider");
  return value;
}

// The results fade while the next set is loading.
export function PendingResults({ children }: { children: ReactNode }) {
  const { pending } = useSearchNavigation();
  return (
    <div aria-busy={pending} className={cn("transition-opacity", pending && "pointer-events-none opacity-50")}>
      {children}
    </div>
  );
}
