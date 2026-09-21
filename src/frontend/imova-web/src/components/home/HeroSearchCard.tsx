"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils/cn";

const LISTING_TYPES = ["Sale", "Rent"] as const;
const LOCATIONS = ["Chisinau", "Balti", "Cahul", "Orhei", "Ungheni", "Soroca"] as const;
const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;

const fieldLabelClass = "mb-1 block text-[11px] font-bold uppercase tracking-wide text-white/70 sm:mb-1.5 sm:text-xs";
const fieldClass =
  "h-11 w-full rounded-xl border border-white/20 bg-white/90 px-3.5 text-sm text-ink-900 outline-none transition-colors placeholder:text-ink-400 focus:border-white focus:bg-white focus:ring-2 focus:ring-white/30 sm:h-12";

// The homepage search/filtering backend doesn't exist yet (see CLAUDE.md's known gaps) — this is
// UI only for now. Sits directly on the hero image (see Hero.tsx), glass-styled to match — not a
// separate white card floating below it. Buy/Rent tab state is local and cosmetic; submitting
// logs instead of navigating until a real search endpoint exists.
export function HeroSearchCard() {
  const t = useTranslations("Hero.searchCard");
  const tType = useTranslations("PropertyType");
  const [listingType, setListingType] = useState<(typeof LISTING_TYPES)[number]>("Sale");

  function handleSearch() {
    console.log("Hero search (not wired up yet):", { listingType });
  }

  return (
    <div className="rounded-[22px] border border-white/15 bg-white/10 p-3.5 shadow-[0_24px_60px_-16px_rgba(0,0,0,0.5)] backdrop-blur-md sm:p-6">
      <div className="mb-3 inline-flex rounded-full border border-white/15 bg-white/10 p-1 sm:mb-4">
        {LISTING_TYPES.map((type) => (
          <button
            key={type}
            type="button"
            onClick={() => setListingType(type)}
            className={cn(
              "rounded-full px-4 py-1.5 text-sm font-semibold transition-colors sm:px-5 sm:py-2",
              listingType === type ? "bg-brand-500 text-white" : "text-white/70 hover:text-white",
            )}
          >
            {type === "Sale" ? t("buyTab") : t("rentTab")}
          </button>
        ))}
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4 lg:grid-cols-[1.2fr_1.2fr_1fr_1fr_auto] lg:items-end">
        <label className="block text-left">
          <span className={fieldLabelClass}>{t("locationLabel")}</span>
          <select className={fieldClass} defaultValue="Chisinau">
            {LOCATIONS.map((loc) => (
              <option key={loc} value={loc}>
                {t(`locations.${loc}`)}
              </option>
            ))}
          </select>
        </label>

        <label className="block text-left">
          <span className={fieldLabelClass}>{t("typeLabel")}</span>
          <select className={fieldClass} defaultValue="">
            <option value="">{t("anyType")}</option>
            {PROPERTY_TYPES.map((type) => (
              <option key={type} value={type}>
                {tType(type)}
              </option>
            ))}
          </select>
        </label>

        <label className="block text-left">
          <span className={fieldLabelClass}>{t("minPriceLabel")}</span>
          <input type="number" inputMode="numeric" placeholder={t("minPricePlaceholder")} className={fieldClass} />
        </label>

        <label className="block text-left">
          <span className={fieldLabelClass}>{t("maxPriceLabel")}</span>
          <input type="number" inputMode="numeric" placeholder={t("maxPricePlaceholder")} className={fieldClass} />
        </label>

        <button
          type="button"
          onClick={handleSearch}
          className="flex h-11 w-full items-center justify-center gap-2 rounded-xl bg-accent-500 px-6 text-sm font-semibold text-white transition-colors hover:bg-accent-600 sm:h-12 lg:w-auto"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-4 w-4 shrink-0">
            <circle cx="11" cy="11" r="7" />
            <path d="m20 20-3.5-3.5" strokeLinecap="round" />
          </svg>
          {t("searchButton")}
        </button>
      </div>
    </div>
  );
}
