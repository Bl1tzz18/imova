"use client";

import Link from "next/link";
import { useState, useTransition } from "react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils/cn";
import { updateEmailPreferences } from "@/lib/account/actions";
import type { EmailPreferences } from "@/lib/account/emailPreferences";

// The "Notifications" tab of /account: the optional emails, each a switch that saves right away.
// Saved-search alerts are set per search on /saved-searches, so this only links there.
export function NotificationsPanel({ initial }: { initial: EmailPreferences | null }) {
  const t = useTranslations("Account");
  const [preferences, setPreferences] = useState(initial);
  const [status, setStatus] = useState<{ kind: "saved" | "error"; text: string } | null>(null);
  const [pending, startTransition] = useTransition();

  if (!preferences) {
    return <p className="text-sm text-ink-500">{t("notificationsUnavailable")}</p>;
  }

  const toggleFavoriteUpdates = () => {
    const next = { ...preferences, favoriteUpdates: !preferences.favoriteUpdates };
    const previous = preferences;
    setPreferences(next);
    setStatus(null);
    startTransition(async () => {
      const result = await updateEmailPreferences(next);
      if ("error" in result) {
        setPreferences(previous);
        setStatus({ kind: "error", text: result.error });
      } else {
        setPreferences(result.preferences);
        setStatus({ kind: "saved", text: t("notificationsSaved") });
      }
    });
  };

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-1">
        <h2 className="text-base font-semibold text-ink-950">{t("notificationsTitle")}</h2>
        <p className="text-sm text-ink-500">{t("notificationsBody")}</p>
      </section>

      <div className="flex items-start justify-between gap-4 rounded-xl border border-ink-100 px-4 py-4 sm:px-5">
        <div className="min-w-0">
          <p id="favorite-updates-label" className="text-sm font-medium text-ink-900">
            {t("favoriteUpdatesLabel")}
          </p>
          <p id="favorite-updates-hint" className="mt-1 text-sm text-ink-500">
            {t("favoriteUpdatesHint")}
          </p>
          <p
            role="status"
            aria-live="polite"
            className={cn("mt-2 text-sm empty:hidden", status?.kind === "error" ? "text-accent-700" : "text-brand-700")}
          >
            {status?.text}
          </p>
        </div>
        <button
          type="button"
          role="switch"
          aria-checked={preferences.favoriteUpdates}
          aria-labelledby="favorite-updates-label"
          aria-describedby="favorite-updates-hint"
          disabled={pending}
          onClick={toggleFavoriteUpdates}
          // A 44px-tall hit area around the 24px track, for touch.
          className="-my-2.5 inline-flex h-11 w-14 shrink-0 items-center justify-center rounded-full focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/40 disabled:opacity-60"
        >
          <span
            aria-hidden
            className={cn(
              "flex h-6 w-11 items-center rounded-full p-0.5 transition-colors",
              preferences.favoriteUpdates ? "bg-brand-600" : "bg-ink-200",
            )}
          >
            <span
              className={cn(
                "h-5 w-5 rounded-full bg-white shadow transition-transform motion-reduce:transition-none",
                preferences.favoriteUpdates ? "translate-x-5" : "translate-x-0",
              )}
            />
          </span>
        </button>
      </div>

      <p className="border-t border-ink-100 pt-5 text-sm text-ink-500">
        {t.rich("savedSearchAlertsHint", {
          link: (chunks) => (
            <Link href="/saved-searches" className="font-medium text-brand-700 underline-offset-2 hover:underline">
              {chunks}
            </Link>
          ),
        })}
      </p>
    </div>
  );
}
