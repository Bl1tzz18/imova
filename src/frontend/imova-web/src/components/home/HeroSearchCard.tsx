"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { locationParams, searchLocations, type LocationSuggestion } from "@/lib/api/locationSearch";
import { searchHref } from "@/lib/search/filters";
import { LocationAutocomplete } from "./LocationAutocomplete";

// The homepage's one question: where. Buy or rent, type, price — all of that is the /search page's
// job (it has its own switch), so the hero asks nothing else before the first click. One white
// field with a search button, on the hero photo.
export function HeroSearchCard() {
  const t = useTranslations("Hero.searchCard");
  const router = useRouter();
  const [location, setLocation] = useState<LocationSuggestion | null>(null);
  const [locationText, setLocationText] = useState("");
  const [searching, setSearching] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSearching(true);
    // Typed a place without picking it from the list: take the best match.
    const place = location ?? (locationText.trim().length >= 2 ? (await searchLocations(locationText))[0] ?? null : null);
    router.push(searchHref(place ? locationParams(place) : {}));
  }

  return (
    <form
      onSubmit={handleSubmit}
      role="search"
      className="flex items-center gap-2 rounded-xl bg-white p-1.5 shadow-[0_12px_32px_-10px_rgba(0,0,0,0.5)]"
    >
      <div className="min-w-0 flex-1" onInput={(e) => setLocationText((e.target as HTMLInputElement).value)}>
        <LocationAutocomplete
          value={location}
          onChange={setLocation}
          className="h-12 w-full rounded-lg bg-transparent px-3.5 text-base text-ink-900 outline-none placeholder:text-ink-400 sm:h-[52px] sm:px-4"
          placeholder={t("locationPlaceholder")}
          ariaLabel={t("locationLabel")}
        />
      </div>
      <button
        type="submit"
        disabled={searching}
        aria-label={t("searchButton")}
        title={t("searchButton")}
        className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-accent-500 text-white transition-colors hover:bg-accent-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent-500 focus-visible:ring-offset-2 disabled:opacity-70 sm:h-[52px] sm:w-[52px]"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" className="h-5 w-5" aria-hidden>
          <circle cx="11" cy="11" r="7" />
          <path d="m20 20-3.5-3.5" strokeLinecap="round" />
        </svg>
      </button>
    </form>
  );
}
