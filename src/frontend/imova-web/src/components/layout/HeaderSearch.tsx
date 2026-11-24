"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { usePathname, useRouter } from "@/i18n/navigation";
import { LocationAutocomplete } from "@/components/home/LocationAutocomplete";
import { locationParams, searchLocations, type LocationSuggestion } from "@/lib/api/locationSearch";
import { showsHeaderSearch } from "@/lib/navigation/tabBar";
import { searchHref } from "@/lib/search/filters";

// The phone header's "where?" field (999.md's search box, for places): pick a raion, locality or
// Chișinău sector — or type one and press Enter for the best match — and the results open. Not on
// /search and /map: those have their own location field, which edits the current search.
export function HeaderSearch() {
  const t = useTranslations("Header");
  const router = useRouter();
  const pathname = usePathname();
  const [text, setText] = useState("");

  if (!showsHeaderSearch(pathname)) return <span className="flex-1" />;

  function open(place: LocationSuggestion | null) {
    router.push(searchHref(place ? locationParams(place) : {}));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    open(text.trim().length >= 2 ? (await searchLocations(text))[0] ?? null : null);
  }

  return (
    <form role="search" onSubmit={handleSubmit} className="relative min-w-0 flex-1" onInput={(e) => setText((e.target as HTMLInputElement).value)}>
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="pointer-events-none absolute left-3 top-1/2 z-10 h-4 w-4 -translate-y-1/2 text-ink-400" aria-hidden>
        <circle cx="11" cy="11" r="7" />
        <path d="m20 20-3.5-3.5" strokeLinecap="round" />
      </svg>
      <LocationAutocomplete
        key={pathname}
        value={null}
        onChange={(picked) => picked && open(picked)}
        className="h-10 w-full rounded-full border border-ink-200 bg-white pl-9 pr-3 text-base text-ink-900 outline-none placeholder:text-sm placeholder:text-ink-400 focus:border-brand-400 focus:ring-2 focus:ring-brand-500/20"
        placeholder={t("searchPlaceholder")}
        ariaLabel={t("searchLabel")}
      />
    </form>
  );
}
