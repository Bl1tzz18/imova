"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { locationParams, searchLocations, type LocationSuggestion } from "@/lib/api/locationSearch";
import { searchHref, TRANSACTION_TYPES, type SearchState } from "@/lib/search/filters";
import { cn } from "@/lib/utils/cn";
import { LocationAutocomplete } from "./LocationAutocomplete";

// The homepage's one question: buy or rent, and where. Everything else (type, price, rooms) is the
// /search page's job — every extra field here would lose people before their first click. A white
// bar on the hero's dark ground; on a phone the switch, the field and the button stack.
export function HeroSearchCard() {
  const t = useTranslations("Hero.searchCard");
  const router = useRouter();
  const [transactionType, setTransactionType] = useState<(typeof TRANSACTION_TYPES)[number]>("Sale");
  const [location, setLocation] = useState<LocationSuggestion | null>(null);
  const [locationText, setLocationText] = useState("");
  const [searching, setSearching] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSearching(true);
    // Typed a place without picking it from the list: take the best match.
    const place = location ?? (locationText.trim().length >= 2 ? (await searchLocations(locationText))[0] ?? null : null);
    const state: SearchState = {
      transactionType: [transactionType],
      ...(place ? locationParams(place) : {}),
    };
    router.push(searchHref(state));
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="flex flex-col gap-2 rounded-2xl bg-white p-2 shadow-[0_24px_48px_-20px_rgba(0,0,0,0.55)] sm:flex-row sm:items-center"
    >
      <div className="inline-flex shrink-0 self-start rounded-full bg-ink-100 p-1 sm:self-center" role="radiogroup" aria-label={t("transactionLabel")}>
        {TRANSACTION_TYPES.map((type) => (
          <button
            key={type}
            type="button"
            role="radio"
            aria-checked={transactionType === type}
            onClick={() => setTransactionType(type)}
            className={cn(
              "rounded-full px-4 py-2 text-sm font-semibold transition-colors",
              transactionType === type ? "bg-white text-ink-950 shadow-sm" : "text-ink-500 hover:text-ink-900",
            )}
          >
            {type === "Sale" ? t("buyTab") : t("rentTab")}
          </button>
        ))}
      </div>

      <div
        className="relative min-w-0 flex-1 border-t border-ink-100 sm:border-l sm:border-t-0"
        onInput={(e) => setLocationText((e.target as HTMLInputElement).value)}
      >
        <svg
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          className="pointer-events-none absolute left-3 top-1/2 z-10 h-4 w-4 -translate-y-1/2 text-ink-400"
          aria-hidden
        >
          <circle cx="11" cy="11" r="7" />
          <path d="m20 20-3.5-3.5" strokeLinecap="round" />
        </svg>
        <LocationAutocomplete
          value={location}
          onChange={setLocation}
          className="h-12 w-full bg-transparent pl-10 pr-3 text-[15px] text-ink-900 outline-none placeholder:text-ink-400"
          placeholder={t("locationPlaceholder")}
          ariaLabel={t("locationLabel")}
        />
      </div>

      <button
        type="submit"
        disabled={searching}
        className="flex h-12 shrink-0 items-center justify-center rounded-xl bg-accent-500 px-6 text-sm font-semibold text-white transition-colors hover:bg-accent-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent-500 focus-visible:ring-offset-2 disabled:opacity-70"
      >
        {t("searchButton")}
      </button>
    </form>
  );
}
