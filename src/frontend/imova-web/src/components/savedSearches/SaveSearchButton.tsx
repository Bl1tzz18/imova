"use client";

import { useEffect, useRef, useState, useTransition } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Button, LinkButton } from "@/components/ui/Button";
import { FieldLabel, SelectInput, TextInput } from "@/components/ui/Field";
import { saveSearch } from "@/lib/savedSearches/actions";
import { ALERT_FREQUENCIES, SAVED_SEARCHES_PATH, isAlertFrequency, type AlertFrequency } from "@/lib/savedSearches/savedSearch";

function BookmarkIcon() {
  return (
    <svg viewBox="0 0 24 24" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden>
      <path d="M6.5 3.5h11a1 1 0 0 1 1 1V21l-6.5-4.5L5.5 21V4.5a1 1 0 0 1 1-1Z" strokeLinejoin="round" />
    </svg>
  );
}

// "Save this search" in the results toolbar of /search and /map. Saves the current filters (see
// queryToSave) under a name — the page's own title by default — with email alerts (daily unless
// changed). Saving the same filters again just updates that saved search. Signed-out visitors go
// to sign in first and come back to this search.
export function SaveSearchButton({
  queryString,
  defaultName,
  signedIn,
  returnTo,
}: {
  queryString: string;
  defaultName: string;
  signedIn: boolean;
  returnTo: string;
}) {
  const t = useTranslations("SavedSearches");
  const [open, setOpen] = useState(false);
  const [name, setName] = useState(defaultName);
  const [frequency, setFrequency] = useState<AlertFrequency>("Daily");
  const [status, setStatus] = useState<{ saved?: boolean; error?: string }>({});
  const [pending, startTransition] = useTransition();
  const containerRef = useRef<HTMLDivElement>(null);

  // A different search (filters changed while the panel was closed) starts from its own title.
  useEffect(() => {
    if (!open) {
      setName(defaultName);
      setStatus({});
    }
  }, [defaultName, queryString, open]);

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
      <LinkButton href={`/login?next=${encodeURIComponent(returnTo)}`} variant="secondary" size="sm">
        <BookmarkIcon />
        {t("save")}
      </LinkButton>
    );
  }

  function submit(e: React.FormEvent) {
    e.preventDefault();
    startTransition(async () => {
      const result = await saveSearch(name.trim() || defaultName, queryString, frequency);
      setStatus(result.error ? { error: result.error } : { saved: true });
    });
  }

  return (
    <div ref={containerRef} className="relative">
      <Button type="button" variant="secondary" size="sm" onClick={() => setOpen((v) => !v)} aria-expanded={open}>
        <BookmarkIcon />
        {t("save")}
      </Button>

      {open && (
        <div className="absolute right-0 top-[calc(100%+8px)] z-30 w-80 rounded-2xl border border-ink-100 bg-white p-4 shadow-[var(--shadow-card)]">
          {status.saved ? (
            <div className="flex flex-col gap-3 text-sm">
              <p className="font-medium text-emerald-700">{t("saved")}</p>
              <p className="text-ink-500">{frequency === "Off" ? t("savedNoAlerts") : t(`savedWithAlerts.${frequency}`)}</p>
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
