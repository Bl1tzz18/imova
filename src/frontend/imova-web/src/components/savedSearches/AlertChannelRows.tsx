"use client";

import { useState } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { siTelegram } from "simple-icons";
import { cn } from "@/lib/utils/cn";
import { toggleAlertChannel, type AlertChannels } from "@/lib/savedSearches/savedSearch";

export type TelegramChannelStatus = {
  // The site has a bot (otherwise the Telegram row isn't shown).
  available: boolean;
  connected: boolean;
  // "@username" or the first name Telegram gave; null when unknown.
  name: string | null;
};

function MailIcon() {
  return (
    <svg viewBox="0 0 24 24" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden>
      <rect x="3.5" y="5.5" width="17" height="13" rx="2" />
      <path d="m4 7 8 6 8-6" strokeLinejoin="round" />
    </svg>
  );
}

function TelegramBadge() {
  return (
    <span aria-hidden className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full" style={{ backgroundColor: `#${siTelegram.hex}` }}>
      <svg viewBox="0 0 24 24" fill="white" className="h-4 w-4">
        <path d={siTelegram.path} />
      </svg>
    </span>
  );
}

// "Primește alertele pe": where a saved search's alerts go, one row per place — a checkbox, its
// icon, its name and the exact address under it (the account's email, the Telegram name). Pick one
// or both; unticking the last one is refused with a hint (no alerts at all is "Fără alerte"). Without
// Telegram connected its row has a "Conectează" action where the checkbox would be: onConnect (the
// save panel connects in place) or connectHref (a link to the account settings).
export function AlertChannelRows({
  channels,
  onChange,
  email,
  telegram,
  onConnect,
  connectHref,
  connecting = false,
  disabled = false,
}: {
  channels: AlertChannels;
  onChange: (next: AlertChannels) => void;
  email?: string | null;
  telegram: TelegramChannelStatus;
  onConnect?: () => void;
  connectHref?: string;
  connecting?: boolean;
  disabled?: boolean;
}) {
  const t = useTranslations("SavedSearches");
  const [needOne, setNeedOne] = useState(false);

  const toggle = (channel: keyof AlertChannels) => {
    const next = toggleAlertChannel(channels, channel);
    setNeedOne(next === null);
    if (next) onChange(next);
  };

  // The ticked state is drawn on the row; the real checkbox stays for keyboard and screen readers.
  const checkbox = (channel: keyof AlertChannels, label: string) => (
    <span className="relative flex h-5 w-5 shrink-0 items-center justify-center">
      <input
        type="checkbox"
        aria-label={label}
        checked={channels[channel]}
        disabled={disabled}
        onChange={() => toggle(channel)}
        className="peer absolute inset-0 cursor-pointer appearance-none rounded-[6px] border-[1.5px] border-ink-300 bg-white transition-colors checked:border-brand-600 checked:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/40 disabled:cursor-default disabled:opacity-60"
      />
      <svg viewBox="0 0 24 24" className="pointer-events-none relative hidden h-3.5 w-3.5 peer-checked:block" fill="none" stroke="white" strokeWidth="3" aria-hidden>
        <path d="m5 12.5 4.5 4.5L19 7.5" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    </span>
  );

  const connectClass =
    "inline-flex h-8 shrink-0 items-center rounded-full border border-[#229ED9]/50 px-3 text-[13px] font-medium text-[#1b8cc4] transition-colors hover:bg-[#229ED9]/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#229ED9]/40 disabled:opacity-60";

  return (
    <div role="group" aria-label={t("channelsLabel")}>
      <p className="mb-1.5 text-sm font-medium text-ink-700">{t("channelsLabel")}</p>
      <div className="overflow-hidden rounded-xl border border-ink-100 bg-white">
        <label className={cn("flex items-center gap-3 px-3 py-2.5", !disabled && "cursor-pointer hover:bg-ink-50/60")}>
          {checkbox("email", t("channelEmail"))}
          <span aria-hidden className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-ink-50 text-ink-600">
            <MailIcon />
          </span>
          <span className="min-w-0 flex-1">
            <span className="block text-sm font-medium text-ink-900">{t("channelEmail")}</span>
            <span className="block truncate text-xs text-ink-500">{email || t("channelEmailHint")}</span>
          </span>
        </label>

        {telegram.available && (
          <div className="border-t border-ink-100">
            {telegram.connected ? (
              <label className={cn("flex items-center gap-3 px-3 py-2.5", !disabled && "cursor-pointer hover:bg-ink-50/60")}>
                {checkbox("telegram", t("channelTelegram"))}
                <TelegramBadge />
                <span className="min-w-0 flex-1">
                  <span className="block text-sm font-medium text-ink-900">{t("channelTelegram")}</span>
                  <span className="block truncate text-xs text-ink-500">{telegram.name ?? t("channelTelegramConnected")}</span>
                </span>
              </label>
            ) : (
              <div className="flex items-center gap-3 px-3 py-2.5">
                {/* No checkbox yet: the same width keeps the icons in one column. */}
                <span aria-hidden className="h-5 w-5 shrink-0" />
                <TelegramBadge />
                <span className="min-w-0 flex-1">
                  <span className="block text-sm font-medium text-ink-900">{t("channelTelegram")}</span>
                  <span className="block text-xs leading-snug text-ink-500">{t("channelTelegramPitch")}</span>
                </span>
                {onConnect ? (
                  <button type="button" onClick={onConnect} disabled={connecting || disabled} className={connectClass}>
                    {t("channelConnect")}
                  </button>
                ) : connectHref ? (
                  <Link href={connectHref} className={connectClass}>
                    {t("channelConnect")}
                  </Link>
                ) : null}
              </div>
            )}
          </div>
        )}
      </div>
      {needOne && (
        <p className="mt-1.5 text-xs text-accent-700" role="status">
          {t("channelsAtLeastOne")}
        </p>
      )}
    </div>
  );
}
