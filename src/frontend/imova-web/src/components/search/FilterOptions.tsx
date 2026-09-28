"use client";

import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { getAmenities } from "@/lib/api/amenities";
import { getChisinauSectors, getLocalitati, getRaioane, type ChisinauSector, type Localitate, type Raion } from "@/lib/api/locations";
import { getProximities } from "@/lib/api/proximities";
import type { Amenity, Proximity } from "@/types/listing";
import { useSearchNavigation } from "./SearchNavigation";

// The option lists the search page's filters choose from — loaded once for the whole page, since
// the top bar, the filter drawer and the applied-filter chips all need them.
type FilterOptions = {
  raioane: Raion[];
  sectors: ChisinauSector[];
  // Of the selected raion (empty without one).
  localitati: Localitate[];
  amenities: Amenity[];
  proximities: Proximity[];
};

const FilterOptionsContext = createContext<FilterOptions | null>(null);

export function FilterOptionsProvider({ children }: { children: ReactNode }) {
  const { state } = useSearchNavigation();
  const raionId = state.raionId?.[0] ?? "";
  const [options, setOptions] = useState<Omit<FilterOptions, "localitati">>({ raioane: [], sectors: [], amenities: [], proximities: [] });
  const [localitati, setLocalitati] = useState<Localitate[]>([]);

  useEffect(() => {
    const set = <K extends keyof typeof options>(key: K) => (value: (typeof options)[K]) => setOptions((prev) => ({ ...prev, [key]: value }));
    getRaioane().then(set("raioane")).catch(() => {});
    getChisinauSectors().then(set("sectors")).catch(() => {});
    getAmenities().then(set("amenities")).catch(() => {});
    getProximities().then(set("proximities")).catch(() => {});
  }, []);

  useEffect(() => {
    if (!raionId) {
      setLocalitati([]);
      return;
    }
    getLocalitati(raionId).then(setLocalitati).catch(() => setLocalitati([]));
  }, [raionId]);

  return <FilterOptionsContext.Provider value={{ ...options, localitati }}>{children}</FilterOptionsContext.Provider>;
}

export function useFilterOptions(): FilterOptions {
  const value = useContext(FilterOptionsContext);
  if (!value) throw new Error("useFilterOptions must be used inside FilterOptionsProvider");
  return value;
}
