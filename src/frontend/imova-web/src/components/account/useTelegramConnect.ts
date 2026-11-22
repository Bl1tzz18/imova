"use client";

import { useEffect, useState, useTransition } from "react";
import { getTelegramConnection, startTelegramLink } from "@/lib/account/actions";
import { telegramLinkExpired, type TelegramConnection, type TelegramLinkStart } from "@/lib/account/telegram";

// How often we ask whether the user has pressed Start in Telegram yet.
const POLL_MS = 3000;

// Connecting Telegram from anywhere on the site (the account's Notifications tab, the "save this
// search" panel). Two steps on purpose: start() asks for a one-time link, and the caller shows it as
// a real link — opening it by script after the request returns is blocked as a popup in most
// browsers. While the link is out, this polls until the bot has connected the chat (justConnected)
// or the link has expired.
export function useTelegramConnect(initial: TelegramConnection | null) {
  const [connection, setConnection] = useState(initial);
  const [link, setLink] = useState<TelegramLinkStart | null>(null);
  const [expired, setExpired] = useState(false);
  const [justConnected, setJustConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
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
        setJustConnected(true);
      }
    }, POLL_MS);
    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, [link]);

  const start = () => {
    setError(null);
    setExpired(false);
    setJustConnected(false);
    startTransition(async () => {
      const result = await startTelegramLink();
      if ("error" in result) {
        setError(result.error);
      } else {
        setLink(result.link);
      }
    });
  };

  const cancel = () => {
    setLink(null);
    setExpired(false);
  };

  return { connection, setConnection, link, expired, justConnected, error, setError, pending, start, cancel };
}
