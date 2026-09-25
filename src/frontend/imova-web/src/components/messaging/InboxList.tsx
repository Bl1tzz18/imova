"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useIsClient } from "@/lib/hooks/useIsClient";
import { applyIncomingMessage, markConversationRead } from "@/lib/messaging/inbox";
import { RealtimeEvents } from "@/lib/messaging/realtime";
import { messagePreview } from "@/lib/messaging/thread";
import { cn } from "@/lib/utils/cn";
import type { ConversationSummary, Message } from "@/types/messaging";
import { useRealtimeEvent } from "./RealtimeProvider";

// Rows split by thin dividers. Unread rows get an accent left border, a tinted background and an
// orange dot; read ones a gray double tick. "card": the inbox page's standalone list. "sidebar": the
// list next to an open conversation, which is highlighted (and counts as read — it's on screen).
export function InboxList({
  conversations,
  currentUserId,
  activeConversationId = null,
  variant = "card",
}: {
  conversations: ConversationSummary[];
  currentUserId: string;
  activeConversationId?: string | null;
  variant?: "card" | "sidebar";
}) {
  const t = useTranslations("Messages");
  const locale = useLocale();
  const router = useRouter();
  const withActiveRead = (list: ConversationSummary[]) =>
    activeConversationId ? markConversationRead(list, activeConversationId) : list;
  const [items, setItems] = useState(() => withActiveRead(conversations));
  // Times are shown in the viewer's time zone, which only the browser knows.
  const isClient = useIsClient();

  // eslint-disable-next-line react-hooks/exhaustive-deps -- re-seed only when the server list changes
  useEffect(() => setItems(withActiveRead(conversations)), [conversations, activeConversationId]);

  useRealtimeEvent<Message>(RealtimeEvents.MessageReceived, (message) => {
    const updated = applyIncomingMessage(items, message, currentUserId, activeConversationId);
    // A conversation that isn't listed yet (new, or archived): let the server rebuild the list.
    if (updated) setItems(updated);
    else router.refresh();
  });

  const today = new Date().toDateString();
  const time = new Intl.DateTimeFormat(locale, { hour: "2-digit", minute: "2-digit" });
  const date = new Intl.DateTimeFormat(locale, { day: "numeric", month: "short" });

  return (
    <ul
      className={cn(
        "divide-y divide-line bg-white",
        variant === "card" && "overflow-hidden rounded-[18px] border border-line shadow-[var(--shadow-card)]",
        variant === "sidebar" && "border-b border-line",
      )}
    >
      {items.map((c) => {
        const at = new Date(c.lastMessageAt);
        const unread = c.unreadCount > 0;
        const active = c.id === activeConversationId;
        const deleted = c.listing.title === null;
        const fromMe = c.lastMessage?.senderUserId === currentUserId;
        return (
          <li key={c.id}>
            <Link
              href={`/messages/${c.id}`}
              aria-label={unread ? `${c.otherParticipant.displayName} — ${t("unreadCount", { count: c.unreadCount })}` : undefined}
              aria-current={active ? "page" : undefined}
              className={cn(
                "flex items-center gap-3.5 border-l-[3px] px-4 py-3.5 transition-colors",
                active
                  ? "border-accent-500 bg-canvas"
                  : unread
                    ? "border-accent-500 bg-accent-100/30 hover:bg-accent-100/60"
                    : "border-transparent hover:bg-bubble",
              )}
            >
              <div className="flex h-[58px] w-[58px] shrink-0 items-center justify-center overflow-hidden rounded-[14px] bg-bubble text-ink-300">
                {c.listing.photoUrl && !deleted ? (
                  // eslint-disable-next-line @next/next/no-img-element -- blob storage URL
                  <img src={c.listing.photoUrl} alt="" className="h-full w-full object-cover" />
                ) : (
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" className="h-7 w-7" aria-hidden>
                    <path d="M3.5 11 12 4l8.5 7M5.5 9.5V20h13V9.5M10 20v-5h4v5" strokeLinecap="round" strokeLinejoin="round" />
                  </svg>
                )}
              </div>

              <div className="min-w-0 flex-1">
                <div className="flex items-baseline justify-between gap-3">
                  <p className={cn("truncate text-[15px] text-ink-950", unread ? "font-bold" : "font-semibold")}>
                    {c.otherParticipant.displayName}
                  </p>
                  <span className={cn("shrink-0 text-xs", unread ? "font-medium text-accent-600" : "text-ink-400")}>
                    {isClient && (at.toDateString() === today ? time.format(at) : date.format(at))}
                  </span>
                </div>
                <p className={cn("truncate text-xs", deleted ? "text-ink-400" : "font-medium text-accent-600")}>
                  {c.listing.title ?? t("listingDeleted")}
                </p>
                <div className="mt-0.5 flex items-center justify-between gap-3">
                  <p className={cn("truncate text-sm", unread ? "text-ink-900" : "text-ink-500")}>
                    {fromMe && `${t("you")}: `}
                    {messagePreview(c.lastMessage, t("imagePreview"))}
                  </p>
                  {unread ? (
                    <span className="h-2.5 w-2.5 shrink-0 rounded-full bg-accent-500" aria-hidden />
                  ) : (
                    <span className="shrink-0 text-xs font-semibold tracking-[-0.2em] text-ink-300" aria-hidden>
                      ✓✓
                    </span>
                  )}
                </div>
              </div>
            </Link>
          </li>
        );
      })}
    </ul>
  );
}
