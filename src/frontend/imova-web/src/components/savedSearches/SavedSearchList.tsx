"use client";

import { useState, useTransition } from "react";
import { useTranslations } from "next-intl";
import { Button, buttonClassName } from "@/components/ui/Button";
import { SelectInput, TextInput } from "@/components/ui/Field";
import { deleteSavedSearch, updateSavedSearch } from "@/lib/savedSearches/actions";
import {
  ALERT_FREQUENCIES,
  isAlertFrequency,
  openSavedSearchHref,
  type AlertFrequency,
  type SavedSearch,
} from "@/lib/savedSearches/savedSearch";

// One saved search plus what the page computed about it on the server (its title, filter count).
export type SavedSearchRow = SavedSearch & { summary: string };

export function SavedSearchList({ rows }: { rows: SavedSearchRow[] }) {
  return (
    <ul className="flex flex-col gap-3">
      {rows.map((row) => (
        <SavedSearchItem key={row.id} row={row} />
      ))}
    </ul>
  );
}

function SavedSearchItem({ row }: { row: SavedSearchRow }) {
  const t = useTranslations("SavedSearches");
  const [name, setName] = useState(row.name);
  const [frequency, setFrequency] = useState<AlertFrequency>(row.alertFrequency);
  const [renaming, setRenaming] = useState(false);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function save(nextName: string, nextFrequency: AlertFrequency) {
    startTransition(async () => {
      const result = await updateSavedSearch(row.id, nextName, nextFrequency);
      if (result.error !== undefined) {
        setError(result.error);
        return;
      }
      setError(null);
      setName(result.savedSearch.name);
      setFrequency(result.savedSearch.alertFrequency);
      setRenaming(false);
    });
  }

  function remove() {
    startTransition(async () => {
      const result = await deleteSavedSearch(row.id);
      if (result.error) setError(result.error);
    });
  }

  return (
    <li className="rounded-2xl border border-ink-100 bg-white p-5 shadow-[var(--shadow-card)]">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0 flex-1">
          {renaming ? (
            <form
              className="flex gap-2"
              onSubmit={(e) => {
                e.preventDefault();
                save(name.trim() || row.name, frequency);
              }}
            >
              <TextInput value={name} onChange={(e) => setName(e.target.value)} maxLength={100} autoFocus required className="h-9" />
              <Button type="submit" size="sm" disabled={pending}>
                {t("saveSubmit")}
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  setName(row.name);
                  setRenaming(false);
                }}
              >
                {t("cancel")}
              </Button>
            </form>
          ) : (
            <div className="flex flex-wrap items-center gap-2">
              {/* Plain <a>, not next/link: the open route marks the search viewed, and a prefetch would too. */}
              <a href={openSavedSearchHref(row.id)} className="truncate font-semibold text-ink-950 hover:text-brand-700">
                {name}
              </a>
              {row.newListingsCount > 0 && (
                <span className="rounded-full bg-accent-100 px-2 py-0.5 text-xs font-semibold text-accent-700">
                  {t("newCount", { count: row.newListingsCount })}
                </span>
              )}
            </div>
          )}
          <p className="mt-1 text-sm text-ink-500">{row.summary}</p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <label className="flex items-center gap-2 text-sm text-ink-600">
            <span className="sr-only whitespace-nowrap sm:not-sr-only">{t("alertLabel")}</span>
            <SelectInput
              value={frequency}
              disabled={pending}
              className="h-9 w-auto"
              onChange={(e) => isAlertFrequency(e.target.value) && save(name, e.target.value)}
            >
              {ALERT_FREQUENCIES.map((f) => (
                <option key={f} value={f}>
                  {t(`frequency.${f}`)}
                </option>
              ))}
            </SelectInput>
          </label>
          <a href={openSavedSearchHref(row.id)} className={buttonClassName({ size: "sm" })}>
            {t("open")}
          </a>
        </div>
      </div>

      <div className="mt-3 flex items-center gap-4 text-sm">
        {!renaming && (
          <button type="button" onClick={() => setRenaming(true)} className="font-medium text-ink-500 hover:text-ink-900">
            {t("rename")}
          </button>
        )}
        {confirmingDelete ? (
          <span className="flex items-center gap-3">
            <span className="text-ink-600">{t("deleteConfirm")}</span>
            <button type="button" onClick={remove} disabled={pending} className="font-medium text-accent-700 hover:text-accent-600">
              {t("deleteYes")}
            </button>
            <button type="button" onClick={() => setConfirmingDelete(false)} className="font-medium text-ink-500 hover:text-ink-900">
              {t("cancel")}
            </button>
          </span>
        ) : (
          <button type="button" onClick={() => setConfirmingDelete(true)} className="font-medium text-ink-500 hover:text-accent-700">
            {t("delete")}
          </button>
        )}
      </div>

      {error && <p className="mt-2 text-sm text-accent-700">{error}</p>}
    </li>
  );
}
