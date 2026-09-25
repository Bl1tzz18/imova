"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Avatar } from "@/components/ui/Avatar";
import { applyIncomingMessage } from "@/lib/messaging/inbox";
import { RealtimeEvents } from "@/lib/messaging/realtime";
import { messagePreview } from "@/lib/messaging/thread";
import { cn } from "@/lib/utils/cn";
import type { ConversationSummary, Message } from "@/types/messaging";
import { useRealtimeEvent } from "./RealtimeProvider";

export function InboxList({ conversations, currentUserId }: { conversations: ConversationSummary[]; currentUserId: string }) {
  const t = useTranslations("Messages");
  const locale = useLocale();
  const router = useRouter();
  const [items, setItems] = useState(conversations);

  useEffect(() => setItems(conversations), [conversations]);

  useRealtimeEvent<Message>(RealtimeEvents.MessageReceived, (message) => {
    const updated = applyIncomingMessage(items, message, currentUserId);
    // A conversation that isn't listed yet (new, or archived): let the server rebuild the list.
    if (updated) setItems(updated);
    else router.refresh();
  });

  const today = new Date().toDateString();
  const time = new Intl.DateTimeFormat(locale, { hour: "2-digit", minute: "2-digit" });
  const date = new Intl.DateTimeFormat(locale, { day: "numeric", month: "short" });

  return (
    <ul className="divide-y divide-ink-100 overflow-hidden rounded-2xl border border-ink-100 bg-white">
      {items.map((c) => {
        const at = new Date(c.lastMessageAt);
        const unread = c.unreadCount > 0;
        const fromMe = c.lastMessage?.senderUserId === currentUserId;
        return (
          <li key={c.id}>
            <Link href={`/messages/${c.id}`} className="flex items-center gap-3 px-4 py-3 transition-colors hover:bg-ink-50">
              <div className="h-14 w-14 shrink-0 overflow-hidden rounded-xl bg-ink-100">
                {c.listing.photoUrl && (
                  // eslint-disable-next-line @next/next/no-img-element -- blob storage URL
                  <img src={c.listing.photoUrl} alt="" className="h-full w-full object-cover" />
                )}
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-2">
                  <p className={cn("truncate text-sm", unread ? "font-semibold text-ink-950" : "font-medium text-ink-800")}>
                    <span className="inline-flex items-center gap-2 align-middle">
                      <Avatar userId={c.otherParticipant.userId} displayName={c.otherParticipant.displayName} pictureUrl={c.otherParticipant.avatarUrl} size={20} />
                      {c.otherParticipant.displayName}
                    </span>
                  </p>
                  <span className="shrink-0 text-xs text-ink-400">{at.toDateString() === today ? time.format(at) : date.format(at)}</span>
                </div>
                <p className="truncate text-xs text-ink-500">{c.listing.title ?? t("listingDeleted")}</p>
                <div className="flex items-center justify-between gap-2">
                  <p className={cn("truncate text-sm", unread ? "text-ink-900" : "text-ink-500")}>
                    {fromMe && `${t("you")}: `}
                    {messagePreview(c.lastMessage, t("imagePreview"))}
                  </p>
                  {unread && (
                    <span className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-accent-500 px-1.5 text-[11px] font-semibold text-white">
                      {c.unreadCount}
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
