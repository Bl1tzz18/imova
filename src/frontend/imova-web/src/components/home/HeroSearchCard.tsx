"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { locationParams, searchLocations, type LocationSuggestion } from "@/lib/api/locationSearch";
import { PROPERTY_TYPES } from "@/lib/property/attributeSchema";
import { searchHref, TRANSACTION_TYPES, type SearchState } from "@/lib/search/filters";
import { cn } from "@/lib/utils/cn";
import { LocationAutocomplete } from "./LocationAutocomplete";

const fieldLabelClass = "mb-1 block text-[11px] font-bold uppercase tracking-wide text-white/70 sm:mb-1.5 sm:text-xs";
const fieldClass =
  "h-11 w-full rounded-xl border border-white/20 bg-white/90 px-3.5 text-sm text-ink-900 outline-none transition-colors placeholder:text-ink-400 focus:border-white focus:bg-white focus:ring-2 focus:ring-white/30 sm:h-12";

// The homepage's quick entry into search: buy/rent, a property type and a location — then the full
// /search page (with every filter) takes over. Sits on the hero image, glass-styled to match.
export function HeroSearchCard() {
  const t = useTranslations("Hero.searchCard");
  const tType = useTranslations("PropertyType");
  const router = useRouter();
  const [transactionType, setTransactionType] = useState<(typeof TRANSACTION_TYPES)[number]>("Sale");
  const [propertyType, setPropertyType] = useState("");
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
      ...(propertyType ? { propertyType: [propertyType] } : {}),
      ...(place ? locationParams(place) : {}),
    };
    router.push(searchHref(state));
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="rounded-[22px] border border-white/15 bg-white/10 p-3.5 shadow-[0_24px_60px_-16px_rgba(0,0,0,0.5)] backdrop-blur-md sm:p-6"
    >
      <div className="mb-3 inline-flex rounded-full border border-white/15 bg-white/10 p-1 sm:mb-4" role="radiogroup">
        {TRANSACTION_TYPES.map((type) => (
          <button
            key={type}
            type="button"
            role="radio"
            aria-checked={transactionType === type}
            onClick={() => setTransactionType(type)}
            className={cn(
              "rounded-full px-4 py-1.5 text-sm font-semibold transition-colors sm:px-5 sm:py-2",
              transactionType === type ? "bg-brand-500 text-white" : "text-white/70 hover:text-white",
            )}
          >
            {type === "Sale" ? t("buyTab") : t("rentTab")}
          </button>
        ))}
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4 lg:grid-cols-[1fr_1.4fr_auto] lg:items-end">
        <label className="block text-left">
          <span className={fieldLabelClass}>{t("typeLabel")}</span>
          <select className={fieldClass} value={propertyType} onChange={(e) => setPropertyType(e.target.value)}>
            <option value="">{t("anyType")}</option>
            {PROPERTY_TYPES.map((type) => (
              <option key={type} value={type}>
                {tType(type)}
              </option>
            ))}
          </select>
        </label>

        <div className="block text-left" onInput={(e) => setLocationText((e.target as HTMLInputElement).value)}>
          <span className={fieldLabelClass}>{t("locationLabel")}</span>
          <LocationAutocomplete
            value={location}
            onChange={setLocation}
            className={fieldClass}
            placeholder={t("locationPlaceholder")}
            ariaLabel={t("locationLabel")}
          />
        </div>

        <button
          type="submit"
          disabled={searching}
          className="flex h-11 w-full items-center justify-center gap-2 rounded-xl bg-accent-500 px-6 text-sm font-semibold text-white transition-colors hover:bg-accent-600 disabled:opacity-70 sm:col-span-2 sm:h-12 lg:col-span-1 lg:w-auto"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-4 w-4 shrink-0" aria-hidden>
            <circle cx="11" cy="11" r="7" />
            <path d="m20 20-3.5-3.5" strokeLinecap="round" />
          </svg>
          {t("searchButton")}
        </button>
      </div>
    </form>
  );
}
