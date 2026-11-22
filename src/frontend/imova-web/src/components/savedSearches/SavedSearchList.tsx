"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Button, buttonClassName } from "@/components/ui/Button";
import { SelectInput, TextInput } from "@/components/ui/Field";
import { deleteSavedSearch, updateSavedSearch } from "@/lib/savedSearches/actions";
import { cn } from "@/lib/utils/cn";
import {
  ALERT_FREQUENCIES,
  alertChannels,
  isAlertFrequency,
  openSavedSearchHref,
  toggleAlertChannel,
  type AlertChannels,
  type AlertFrequency,
  type SavedSearch,
} from "@/lib/savedSearches/savedSearch";

// One saved search plus what the page computed about it on the server (its title, filter count).
export type SavedSearchRow = SavedSearch & { summary: string };

// The account's Telegram, for the channel choice: available = the site has a bot.
export type TelegramStatus = { available: boolean; connected: boolean };

export function SavedSearchList({ rows, telegram }: { rows: SavedSearchRow[]; telegram: TelegramStatus }) {
  return (
    <ul className="flex flex-col gap-3">
      {rows.map((row) => (
        <SavedSearchItem key={row.id} row={row} telegram={telegram} />
      ))}
    </ul>
  );
}

function CheckIcon() {
  return (
    <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth="2.4" aria-hidden>
      <path d="m5 12.5 4.5 4.5L19 7.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

// A channel the alerts go to, as an on/off chip. The last one on can't be turned off.
function ChannelChip({
  label,
  on,
  locked,
  disabled,
  lockedHint,
  onToggle,
}: {
  label: string;
  on: boolean;
  locked: boolean;
  disabled: boolean;
  lockedHint: string;
  onToggle: () => void;
}) {
  return (
    <button
      type="button"
      aria-pressed={on}
      disabled={disabled || locked}
      title={locked ? lockedHint : undefined}
      onClick={onToggle}
      className={cn(
        "inline-flex h-9 items-center gap-1.5 rounded-full border px-3.5 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/40",
        on ? "border-brand-200 bg-brand-50 text-brand-800" : "border-ink-200 bg-white text-ink-500 hover:border-ink-300 hover:text-ink-800",
        locked ? "cursor-default" : "disabled:opacity-60",
      )}
    >
      {on && <CheckIcon />}
      {label}
    </button>
  );
}

function SavedSearchItem({ row, telegram }: { row: SavedSearchRow; telegram: TelegramStatus }) {
  const t = useTranslations("SavedSearches");
  const [name, setName] = useState(row.name);
  const [frequency, setFrequency] = useState<AlertFrequency>(row.alertFrequency);
  const [channels, setChannels] = useState<AlertChannels>(alertChannels(row));
  const [renaming, setRenaming] = useState(false);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function save(nextName: string, nextFrequency: AlertFrequency, nextChannels?: AlertChannels) {
    startTransition(async () => {
      const result = await updateSavedSearch(row.id, nextName, nextFrequency, nextChannels);
      if (result.error !== undefined) {
        setError(result.error);
        return;
      }
      setError(null);
      setName(result.savedSearch.name);
      setFrequency(result.savedSearch.alertFrequency);
      setChannels(alertChannels(result.savedSearch));
      setRenaming(false);
    });
  }

  function toggle(channel: keyof AlertChannels) {
    const next = toggleAlertChannel(channels, channel);
    if (next) save(name, frequency, next);
  }

  // Telegram is offered once the account has it connected (or when the search already uses it).
  const showTelegram = telegram.connected || channels.telegram;

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

          {frequency !== "Off" && (
            <div className="mt-3 flex flex-wrap items-center gap-2">
              <span className="text-sm text-ink-500">{t("channelsLabel")}</span>
              <ChannelChip
                label={t("channelEmail")}
                on={channels.email}
                locked={channels.email && !channels.telegram}
                disabled={pending}
                lockedHint={t("channelsAtLeastOne")}
                onToggle={() => toggle("email")}
              />
              {showTelegram && (
                <ChannelChip
                  label={t("channelTelegram")}
                  on={channels.telegram}
                  locked={channels.telegram && !channels.email}
                  disabled={pending}
                  lockedHint={t("channelsAtLeastOne")}
                  onToggle={() => toggle("telegram")}
                />
              )}
              {telegram.available && !telegram.connected && (
                <Link
                  href="/account?tab=notifications"
                  className="text-sm font-medium text-brand-700 underline-offset-2 hover:underline"
                >
                  {channels.telegram ? t("telegramNotConnected") : t("connectTelegram")}
                </Link>
              )}
            </div>
          )}
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

      <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm">
        {/* While a delete waits for confirmation, only that question and its two answers show. */}
        {!renaming && !confirmingDelete && (
          <button type="button" onClick={() => setRenaming(true)} className="font-medium text-ink-500 hover:text-ink-900">
            {t("rename")}
          </button>
        )}
        {confirmingDelete ? (
          <span className="flex flex-wrap items-center gap-x-3 gap-y-2">
            <span className="whitespace-nowrap text-ink-600">{t("deleteConfirm")}</span>
            <button type="button" onClick={remove} disabled={pending} className={buttonClassName({ variant: "danger", size: "sm", className: "whitespace-nowrap" })}>
              {t("deleteYes")}
            </button>
            <button type="button" onClick={() => setConfirmingDelete(false)} className="font-medium text-ink-500 hover:text-ink-900">
              {t("cancel")}
            </button>
          </span>
        ) : (
          <button type="button" onClick={() => setConfirmingDelete(true)} className="font-medium text-red-600 hover:text-red-700">
            {t("delete")}
          </button>
        )}
      </div>

      {error && <p className="mt-2 text-sm text-accent-700">{error}</p>}
    </li>
  );
}
