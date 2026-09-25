"use client";

import { useTranslations } from "next-intl";
import { SelectInput } from "@/components/ui/Field";
import { SORTS } from "@/lib/search/filters";
import { useSearchNavigation } from "./SearchNavigation";

export function SortSelect() {
  const t = useTranslations("Search");
  const { state, change } = useSearchNavigation();

  return (
    <label className="flex items-center gap-2 text-sm text-ink-600">
      <span className="hidden sm:inline">{t("sortLabel")}</span>
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
