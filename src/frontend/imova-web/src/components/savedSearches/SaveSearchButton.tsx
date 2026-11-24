"use client";

import { useEffect, useRef, useState, useTransition } from "react";
import { Link } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { siTelegram } from "simple-icons";
import { Button, LinkButton } from "@/components/ui/Button";
import { FieldLabel, SelectInput, TextInput } from "@/components/ui/Field";
import { AlertChannelRows } from "@/components/savedSearches/AlertChannelRows";
import { useTelegramConnect } from "@/components/account/useTelegramConnect";
import { TelegramLinkQr } from "@/components/account/TelegramLinkQr";
import { getTelegramConnection } from "@/lib/account/actions";
import { telegramDisplayName } from "@/lib/account/telegram";
import { saveSearch } from "@/lib/savedSearches/actions";
import { track } from "@/lib/analytics/events";
import {
  ALERT_FREQUENCIES,
  SAVED_SEARCHES_PATH,
  alertChannels,
  alertChannelsKey,
  isAlertFrequency,
  type AlertChannels,
  type AlertFrequency,
  type SavedSearch,
} from "@/lib/savedSearches/savedSearch";

function BookmarkIcon() {
  return (
    <svg viewBox="0 0 24 24" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden>
      <path d="M6.5 3.5h11a1 1 0 0 1 1 1V21l-6.5-4.5L5.5 21V4.5a1 1 0 0 1 1-1Z" strokeLinejoin="round" />
    </svg>
  );
}

// "Save this search" in the results toolbar of /search and /map. Saves the current filters (see
// queryToSave) under a name — the page's own title by default — with alerts (daily unless changed)
// sent by email and/or Telegram. Telegram can be connected right here, without leaving the panel:
// the Telegram row's "Conectează" gives a one-time link, the panel waits while the user presses Start in
// Telegram and then ticks Telegram by itself (useTelegramConnect). The account's Telegram status is
// only fetched when the panel opens, so the search pages don't pay for it. Saving the same filters
// again just updates that saved search. Signed-out visitors go to sign in first and come back.
export function SaveSearchButton({
  queryString,
  defaultName,
  signedIn,
  returnTo,
  email,
}: {
  queryString: string;
  defaultName: string;
  signedIn: boolean;
  returnTo: string;
  // The account's address, shown under "Email" in the channel choice.
  email?: string | null;
}) {
  const t = useTranslations("SavedSearches");
  const [open, setOpen] = useState(false);
  const [name, setName] = useState(defaultName);
  const [frequency, setFrequency] = useState<AlertFrequency>("Daily");
  const [channels, setChannels] = useState<AlertChannels>({ email: true, telegram: false });
  const [status, setStatus] = useState<{ saved?: SavedSearch; error?: string }>({});
  const [pending, startTransition] = useTransition();
  const telegram = useTelegramConnect(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // A different search (filters changed while the panel was closed) starts from its own title.
  useEffect(() => {
    if (!open) {
      setName(defaultName);
      setStatus({});
    }
  }, [defaultName, queryString, open]);

  // The account's Telegram, once per page, the first time the panel opens. Connected → Telegram is
  // ticked from the start, as the API would do for a new search.
  const { connection, setConnection } = telegram;
  const [telegramLoaded, setTelegramLoaded] = useState(false);
  useEffect(() => {
    if (!open || telegramLoaded || !signedIn) return;
    setTelegramLoaded(true);
    getTelegramConnection().then((loaded) => {
      setConnection(loaded);
      if (loaded?.connected) setChannels((c) => ({ ...c, telegram: true }));
    });
  }, [open, telegramLoaded, signedIn, setConnection]);

  // Pressed Start in Telegram while the panel waited: tick Telegram.
  useEffect(() => {
    if (telegram.justConnected) setChannels((c) => ({ ...c, telegram: true }));
  }, [telegram.justConnected]);

  useEffect(() => {
    if (!open) return;
    const close = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) setOpen(false);
    };
    const escape = (e: KeyboardEvent) => e.key === "Escape" && setOpen(false);
    document.addEventListener("mousedown", close);
    document.addEventListener("keydown", escape);
    return () => {
      document.removeEventListener("mousedown", close);
      document.removeEventListener("keydown", escape);
    };
  }, [open]);

  if (!signedIn) {
    return (
      <LinkButton href={`/login?next=${encodeURIComponent(returnTo)}`} variant="secondary" size="sm" className="shrink-0 whitespace-nowrap">
        <BookmarkIcon />
        {t("save")}
      </LinkButton>
    );
  }

  function submit(e: React.FormEvent) {
    e.preventDefault();
    startTransition(async () => {
      const result = await saveSearch(name.trim() || defaultName, queryString, frequency, frequency === "Off" ? undefined : channels);
      setStatus(result.error !== undefined ? { error: result.error } : { saved: result.savedSearch });
      if (result.error === undefined) track("saved_search_created", { alert_frequency: frequency });
    });
  }

  const telegramConnected = connection?.connected ?? false;

  return (
    <div ref={containerRef} className="relative">
      <Button type="button" variant="secondary" size="sm" onClick={() => setOpen((v) => !v)} aria-expanded={open} className="shrink-0 whitespace-nowrap">
        <BookmarkIcon />
        {t("save")}
      </Button>

      {/* Phones: a bottom sheet over a dimmed page (the button may sit anywhere in the toolbar, and
          the form stays within thumb reach). Wider screens: a dropdown under the button. */}
      {open && <div aria-hidden className="fixed inset-0 z-40 bg-ink-950/30 sm:hidden" onClick={() => setOpen(false)} />}
      {open && (
        <div className="fixed inset-x-0 bottom-0 z-50 max-h-[85vh] overflow-y-auto rounded-t-2xl border border-ink-100 bg-white p-4 pb-[max(1rem,env(safe-area-inset-bottom))] shadow-[var(--shadow-card)] sm:absolute sm:inset-x-auto sm:bottom-auto sm:right-0 sm:top-[calc(100%+8px)] sm:z-30 sm:max-h-none sm:w-96 sm:overflow-visible sm:rounded-2xl sm:pb-4">
          {status.saved ? (
            <div className="flex flex-col gap-3 text-sm">
              <p className="font-medium text-emerald-700">{t("saved")}</p>
              <p className="text-ink-500">
                {frequency === "Off"
                  ? t("savedNoAlerts")
                  : t(`savedWithAlerts.${frequency}`, { where: t(`where.${alertChannelsKey(alertChannels(status.saved))}`) })}
              </p>
              <Link href={SAVED_SEARCHES_PATH} className="font-medium text-brand-700 hover:text-brand-800">
                {t("viewAll")}
              </Link>
            </div>
          ) : (
            <form onSubmit={submit} className="flex flex-col gap-3">
              <label className="block">
                <FieldLabel>{t("nameLabel")}</FieldLabel>
                <TextInput value={name} onChange={(e) => setName(e.target.value)} maxLength={100} required />
              </label>
              <label className="block">
                <FieldLabel>{t("alertLabel")}</FieldLabel>
                <SelectInput value={frequency} onChange={(e) => isAlertFrequency(e.target.value) && setFrequency(e.target.value)}>
                  {ALERT_FREQUENCIES.map((f) => (
                    <option key={f} value={f}>
                      {t(`frequency.${f}`)}
                    </option>
                  ))}
                </SelectInput>
              </label>

              {frequency !== "Off" && (
                <div>
                  <AlertChannelRows
                    channels={channels}
                    onChange={setChannels}
                    email={email}
                    telegram={{
                      available: connection?.available ?? false,
                      connected: telegramConnected,
                      name: connection ? telegramDisplayName(connection) : null,
                    }}
                    onConnect={telegram.start}
                    connecting={telegram.pending || telegram.link !== null}
                  />

                  {/* Connecting Telegram right here: open it, press Start, come back — it ticks itself. */}
                  {telegram.link && (
                    <div className="mt-3 rounded-xl bg-ink-50 p-3 text-sm" aria-live="polite">
                      <p className="font-medium text-ink-900">{t("connectTitle")}</p>
                      <p className="mt-1 text-ink-600">{t("connectSteps")}</p>
                      <TelegramLinkQr url={telegram.link.url} className="mt-3" />
                      {/* Telegram's own blue, so it doesn't compete with "Salvează" (the panel's main action). */}
                      <a
                        href={telegram.link.url}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="mt-3 inline-flex h-10 w-full items-center justify-center gap-2 rounded-full bg-[#229ED9] px-4 text-sm font-medium text-white shadow-sm transition-colors hover:bg-[#1c8cc2] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#229ED9]/40 focus-visible:ring-offset-2"
                      >
                        <svg viewBox="0 0 24 24" fill="white" className="h-4 w-4" aria-hidden>
                          <path d={siTelegram.path} />
                        </svg>
                        {t("openTelegram")}
                      </a>
                      <p className="mt-2 flex items-center justify-between gap-2 text-xs text-ink-500">
                        <span className="inline-flex items-center gap-1.5">
                          <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-brand-500 motion-reduce:animate-none" aria-hidden />
                          {t("connectWaiting")}
                        </span>
                        <button type="button" onClick={telegram.cancel} className="font-medium text-ink-500 hover:text-ink-900">
                          {t("notNow")}
                        </button>
                      </p>
                    </div>
                  )}
                  {telegram.justConnected && telegramConnected && (
                    <p className="mt-2 text-sm text-emerald-700" role="status">
                      {t("telegramConnectedHere")}
                    </p>
                  )}
                  {telegram.expired && <p className="mt-2 text-sm text-accent-700">{t("connectExpired")}</p>}
                  {telegram.error && <p className="mt-2 text-sm text-accent-700">{telegram.error}</p>}
                </div>
              )}

              {status.error && <p className="text-sm text-accent-700">{status.error}</p>}
              <div className="flex justify-end gap-2">
                <Button type="button" variant="ghost" size="sm" onClick={() => setOpen(false)}>
                  {t("cancel")}
                </Button>
                <Button type="submit" size="sm" disabled={pending}>
                  {pending ? t("saving") : t("saveSubmit")}
                </Button>
              </div>
            </form>
          )}
        </div>
      )}
    </div>
  );
}
