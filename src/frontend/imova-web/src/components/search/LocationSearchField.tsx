"use client";

import { useTranslations } from "next-intl";
import { LocationAutocomplete } from "@/components/home/LocationAutocomplete";
import { locationParams, type LocationSuggestion } from "@/lib/api/locationSearch";
import { useFilterOptions } from "./FilterOptions";
import { useLocationSummary } from "./FilterControls";
import { useSearchNavigation } from "./SearchNavigation";

const NO_LOCATION = { raionId: null, localitateId: null, chisinauSectorId: null };

// The phone's "where?" field above the results: the home page's typeahead (raion, locality or
// Chișinău sector in one box) instead of the drawer's three dropdowns — the one filter people look
// for first, so it's on screen rather than inside "Filtre". Picking a place applies it; × clears it.
export function LocationSearchField() {
  const t = useTranslations("Search");
  const { state, change } = useSearchNavigation();
  const { raioane } = useFilterOptions();
  const summary = useLocationSummary();

  const raion = raioane.find((r) => r.id === state.raionId?.[0]);
  const sectorId = state.chisinauSectorId?.[0];
  const localitateId = state.localitateId?.[0];
  // The applied location as a suggestion, so the field shows it (and typing replaces it).
  const current: LocationSuggestion | null =
    raion && summary
      ? {
          kind: sectorId ? "Sector" : localitateId ? "Localitate" : "Raion",
          id: sectorId ?? localitateId ?? raion.id,
          name: summary,
          raionId: raion.id,
          raionName: raion.nameRo,
        }
      : null;

  return (
    <div className="relative">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="pointer-events-none absolute left-3.5 top-1/2 z-10 h-4 w-4 -translate-y-1/2 text-ink-400" aria-hidden>
        <circle cx="11" cy="11" r="7" />
        <path d="m20 20-3.5-3.5" strokeLinecap="round" />
      </svg>
      {/* Remounted when the applied location changes elsewhere (the drawer, reset, a chip), so the
          text always shows what's applied. */}
      <LocationAutocomplete
        key={current ? `${current.kind}-${current.id}` : "none"}
        value={current}
        onChange={(picked) => {
          if (picked) change({ ...NO_LOCATION, ...locationParams(picked) });
        }}
        className="h-11 w-full rounded-full border border-line bg-white pl-10 pr-11 text-base text-ink-900 outline-none placeholder:text-ink-400 focus:border-brand-400 focus:ring-2 focus:ring-brand-500/20"
        placeholder={t("locationPlaceholder")}
        ariaLabel={t("location")}
      />
      {current && (
        <button
          type="button"
          onClick={() => change(NO_LOCATION)}
          aria-label={t("clearLocation")}
          title={t("clearLocation")}
          className="absolute right-1.5 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-full text-ink-500 hover:bg-bubble"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-4 w-4" aria-hidden>
            <path d="M6 6l12 12M18 6 6 18" strokeLinecap="round" />
          </svg>
        </button>
      )}
    </div>
  );
}
