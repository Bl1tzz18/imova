"use client";

import { useEffect } from "react";
import { usePathname, useSearchParams } from "next/navigation";
import {
  CURRENT_URL_KEY,
  LAST_SEARCH_KEY,
  PREVIOUS_URL_KEY,
  recordNavigation,
  type NavigationRecord,
} from "@/lib/navigation/history";

function read(): NavigationRecord | null {
  try {
    const current = sessionStorage.getItem(CURRENT_URL_KEY);
    if (!current) return null;
    return {
      current,
      previous: sessionStorage.getItem(PREVIOUS_URL_KEY),
      lastSearch: sessionStorage.getItem(LAST_SEARCH_KEY),
    };
  } catch {
    return null;
  }
}

function write(record: NavigationRecord) {
  try {
    sessionStorage.setItem(CURRENT_URL_KEY, record.current);
    if (record.previous) sessionStorage.setItem(PREVIOUS_URL_KEY, record.previous);
    else sessionStorage.removeItem(PREVIOUS_URL_KEY);
    if (record.lastSearch) sessionStorage.setItem(LAST_SEARCH_KEY, record.lastSearch);
  } catch {
    // Storage blocked (private mode, …): back links fall back to their plain targets.
  }
}

// Remembers, per browser tab, where the visitor has just been and their last search — what the
// "back" links (BackLink) need. Renders nothing.
export function NavigationTracker() {
  const pathname = usePathname();
  const searchParams = useSearchParams();

  useEffect(() => {
    const query = searchParams.toString();
    write(recordNavigation(read(), query ? `${pathname}?${query}` : pathname));
  }, [pathname, searchParams]);

  return null;
}
