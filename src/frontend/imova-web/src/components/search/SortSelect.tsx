"use client";

import { useTranslations } from "next-intl";
import { SelectInput } from "@/components/ui/Field";
import { SORTS } from "@/lib/search/filters";
import { useSearchNavigation } from "./SearchNavigation";

export function SortSelect() {
  const t = useTranslations("Search");
  const { state, change } = useSearchNavigation();

  // No visible "Sortează": the selected option ("Preț crescător") already reads as a sort, and the
  // filter bar needs the room. Screen readers still get the label.
  return (
    <label className="flex items-center text-sm text-ink-600">
      <SelectInput value={state.sort?.[0] ?? "Newest"} onChange={(e) => change({ sort: e.target.value })} className="h-10 w-auto text-sm" aria-label={t("sortLabel")}>
        {SORTS.map((sort) => (
          <option key={sort} value={sort}>
            {t(`sort.${sort}`)}
          </option>
        ))}
      </SelectInput>
    </label>
  );
}
