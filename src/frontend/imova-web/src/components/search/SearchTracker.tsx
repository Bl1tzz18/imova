"use client";

import { useEffect, useRef } from "react";
import { useSearchParams } from "next/navigation";
import { searchEventProperties, track } from "@/lib/analytics/events";

// One search_performed event per search on /search and /map — a new set of filters, not a new
// page or sort of the same one. Renders nothing.
export function SearchTracker({ view, resultCount }: { view: "list" | "map"; resultCount?: number }) {
  const params = useSearchParams();
  const last = useRef<string | null>(null);

  useEffect(() => {
    const properties = searchEventProperties(new URLSearchParams(params.toString()), view);
    const key = JSON.stringify(properties);
    if (last.current === key) return;
    last.current = key;
    track("search_performed", resultCount === undefined ? properties : { ...properties, result_count: resultCount });
  }, [params, view, resultCount]);

  return null;
}
