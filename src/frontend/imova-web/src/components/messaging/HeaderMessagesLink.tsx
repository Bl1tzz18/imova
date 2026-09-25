"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";
import { unreadBadgeLabel } from "@/lib/messaging/thread";
import { useRealtime } from "./RealtimeProvider";

// Envelope icon with the live unread-message count.
export function HeaderMessagesLink() {
  const t = useTranslations("Messages");
  const { unreadCount } = useRealtime();
  const badge = unreadBadgeLabel(unreadCount);

  return (
    <Link
      href="/messages"
      aria-label={badge ? t("inboxWithUnread", { count: unreadCount }) : t("inbox")}
      className="relative flex h-10 w-10 items-center justify-center rounded-full text-ink-600 transition-colors hover:bg-white hover:text-ink-950"
    >
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className="h-5 w-5">
        <path d="M4 6h16v12H4z" strokeLinejoin="round" />
        <path d="m4 7 8 6 8-6" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
      {badge && (
        <span className="absolute -right-0.5 -top-0.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-accent-500 px-1 text-[11px] font-semibold text-white">
          {badge}
        </span>
      )}
    </Link>
  );
}
