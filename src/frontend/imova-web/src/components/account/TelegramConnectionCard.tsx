"use client";

import { useState, useTransition } from "react";
import { useLocale, useTranslations } from "next-intl";
import { siTelegram } from "simple-icons";
import { Button, buttonClassName } from "@/components/ui/Button";
import { useTelegramConnect } from "@/components/account/useTelegramConnect";
import { cn } from "@/lib/utils/cn";
import { formatDate } from "@/lib/utils/format";
import { disconnectTelegram } from "@/lib/account/actions";
import { telegramDisplayName, type TelegramConnection } from "@/lib/account/telegram";

// The Telegram block of the Notifications tab: connect (see useTelegramConnect), or see which
// Telegram is connected and disconnect it.
export function TelegramConnectionCard({ initial }: { initial: TelegramConnection }) {
  const t = useTranslations("Account");
  const locale = useLocale();
  const telegram = useTelegramConnect(initial);
  const connection = telegram.connection ?? initial;
  const [disconnected, setDisconnected] = useState(false);
  const [disconnecting, startDisconnect] = useTransition();

  const disconnect = () => {
    telegram.setError(null);
    setDisconnected(false);
    startDisconnect(async () => {
      const result = await disconnectTelegram();
      if ("error" in result) {
        telegram.setError(result.error);
      } else {
        telegram.setConnection({ ...connection, connected: false, username: null, firstName: null, connectedAt: null });
        setDisconnected(true);
      }
    });
  };

  const name = telegramDisplayName(connection);
  const status = telegram.error
    ? { kind: "error" as const, text: telegram.error }
    : telegram.justConnected && connection.connected
      ? { kind: "saved" as const, text: t("telegramConnectedNow") }
      : disconnected && !connection.connected
        ? { kind: "saved" as const, text: t("telegramDisconnected") }
        : null;

  return (
    <div className="flex flex-col gap-4 rounded-xl border border-ink-100 px-4 py-4 sm:flex-row sm:items-start sm:justify-between sm:px-5">
      <div className="flex min-w-0 gap-3">
        <span
          aria-hidden
          className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full"
          style={{ backgroundColor: `#${siTelegram.hex}` }}
        >
          <svg viewBox="0 0 24 24" fill="white" className="h-5 w-5">
            <path d={siTelegram.path} />
          </svg>
        </span>
        <div className="min-w-0">
          <p className="text-sm font-medium text-ink-900">{t("telegramTitle")}</p>
          <p className="mt-1 text-sm text-ink-500">
            {connection.connected
              ? [
                  name ? t("telegramConnectedAs", { name }) : t("telegramConnected"),
                  connection.connectedAt ? t("telegramConnectedSince", { date: formatDate(connection.connectedAt, locale) }) : null,
                ]
                  .filter(Boolean)
                  .join(" · ")
              : telegram.link
                ? t("telegramWaiting")
                : telegram.expired
                  ? t("telegramExpired")
                  : t("telegramHint")}
          </p>
          <p
            role="status"
            aria-live="polite"
            className={cn("mt-2 text-sm empty:hidden", status?.kind === "error" ? "text-accent-700" : "text-brand-700")}
          >
            {status?.text}
          </p>
        </div>
      </div>

      <div className="flex shrink-0 flex-col gap-2 sm:items-end">
        {connection.connected ? (
          <Button type="button" variant="secondary" disabled={disconnecting} onClick={disconnect} className="w-full sm:w-auto">
            {t("telegramDisconnect")}
          </Button>
        ) : telegram.link ? (
          <>
            <a
              href={telegram.link.url}
              target="_blank"
              rel="noopener noreferrer"
              className={buttonClassName({ className: "w-full sm:w-auto" })}
            >
              {t("telegramOpen")}
            </a>
            <Button type="button" variant="ghost" size="sm" onClick={telegram.cancel} className="w-full sm:w-auto">
              {t("telegramCancel")}
            </Button>
          </>
        ) : (
          <Button type="button" variant="secondary" disabled={telegram.pending} onClick={telegram.start} className="w-full sm:w-auto">
            {t("telegramConnect")}
          </Button>
        )}
      </div>
    </div>
  );
}
