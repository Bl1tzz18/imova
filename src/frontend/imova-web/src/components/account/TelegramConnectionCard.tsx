"use client";

import { useEffect, useState, useTransition } from "react";
import { useLocale, useTranslations } from "next-intl";
import { siTelegram } from "simple-icons";
import { Button, buttonClassName } from "@/components/ui/Button";
import { cn } from "@/lib/utils/cn";
import { formatDate } from "@/lib/utils/format";
import { disconnectTelegram, getTelegramConnection, startTelegramLink } from "@/lib/account/actions";
import {
  telegramDisplayName,
  telegramLinkExpired,
  type TelegramConnection,
  type TelegramLinkStart,
} from "@/lib/account/telegram";

// How often the tab asks whether the user has pressed Start in Telegram yet.
const POLL_MS = 3000;

// The Telegram block of the Notifications tab. Connecting is two steps on purpose: the first button
// asks for a one-time link, the second is a real link to it — a link opened by script after the
// request returns is blocked as a popup in most browsers. While the link is out, the tab polls until
// the bot has connected the chat (or the link has expired).
export function TelegramConnectionCard({ initial }: { initial: TelegramConnection }) {
  const t = useTranslations("Account");
  const locale = useLocale();
  const [connection, setConnection] = useState(initial);
  const [link, setLink] = useState<TelegramLinkStart | null>(null);
  const [expired, setExpired] = useState(false);
  const [status, setStatus] = useState<{ kind: "saved" | "error"; text: string } | null>(null);
  const [pending, startTransition] = useTransition();

  useEffect(() => {
    if (!link) return;
    let cancelled = false;
    const timer = window.setInterval(async () => {
      if (telegramLinkExpired(link)) {
        window.clearInterval(timer);
        if (!cancelled) {
          setLink(null);
          setExpired(true);
        }
        return;
      }

      const latest = await getTelegramConnection();
      if (!cancelled && latest?.connected) {
        window.clearInterval(timer);
        setConnection(latest);
        setLink(null);
        setStatus({ kind: "saved", text: t("telegramConnectedNow") });
      }
    }, POLL_MS);
    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, [link, t]);

  const connect = () => {
    setStatus(null);
    setExpired(false);
    startTransition(async () => {
      const result = await startTelegramLink();
      if ("error" in result) {
        setStatus({ kind: "error", text: result.error });
      } else {
        setLink(result.link);
      }
    });
  };

  const disconnect = () => {
    setStatus(null);
    startTransition(async () => {
      const result = await disconnectTelegram();
      if ("error" in result) {
        setStatus({ kind: "error", text: result.error });
      } else {
        setConnection({ ...connection, connected: false, username: null, firstName: null, connectedAt: null });
        setStatus({ kind: "saved", text: t("telegramDisconnected") });
      }
    });
  };

  const name = telegramDisplayName(connection);

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
              : link
                ? t("telegramWaiting")
                : expired
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
          <Button type="button" variant="secondary" disabled={pending} onClick={disconnect} className="w-full sm:w-auto">
            {t("telegramDisconnect")}
          </Button>
        ) : link ? (
          <>
            <a
              href={link.url}
              target="_blank"
              rel="noopener noreferrer"
              className={buttonClassName({ className: "w-full sm:w-auto" })}
            >
              {t("telegramOpen")}
            </a>
            <Button type="button" variant="ghost" size="sm" onClick={() => setLink(null)} className="w-full sm:w-auto">
              {t("telegramCancel")}
            </Button>
          </>
        ) : (
          <Button type="button" variant="secondary" disabled={pending} onClick={connect} className="w-full sm:w-auto">
            {t("telegramConnect")}
          </Button>
        )}
      </div>
    </div>
  );
}
