"use client";

import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { Avatar } from "@/components/ui/Avatar";
import { useIsClient } from "@/lib/hooks/useIsClient";
import { loadOlderMessages, markConversationRead, sendMessage } from "@/lib/messaging/actions";
import { RealtimeEvents } from "@/lib/messaging/realtime";
import { applyStatusChange, isTypingVisible, mergeMessages, relativeDay, startsNewDay, TYPING_DISPLAY_MS } from "@/lib/messaging/thread";
import { cn } from "@/lib/utils/cn";
import type { ConversationThread, Message, MessageStatusChange, PresenceEvent, TypingEvent } from "@/types/messaging";
import { ConversationActions } from "./ConversationActions";
import { MessageBubble } from "./MessageBubble";
import { MessageComposer } from "./MessageComposer";
import { useRealtime, useRealtimeEvent } from "./RealtimeProvider";

// Fills its container (the conversation page gives it the space next to the sidebar).
// backHref: a back arrow in the header, shown only where the sidebar is hidden (small screens).
export function ThreadView({
  thread,
  currentUserId,
  backHref,
}: {
  thread: ConversationThread;
  currentUserId: string;
  backHref?: string;
}) {
  const t = useTranslations("Messages");
  const locale = useLocale();
  const { connection, connected } = useRealtime();
  const conversation = thread.conversation;
  const other = conversation.otherParticipant;

  const [messages, setMessages] = useState<Message[]>(thread.messages);
  const [hasMore, setHasMore] = useState(thread.hasMore);
  const [loadingOlder, setLoadingOlder] = useState(false);
  const [blockedByMe, setBlockedByMe] = useState(thread.blockedByMe);
  const [otherOnline, setOtherOnline] = useState(false);
  const [typingAt, setTypingAt] = useState<number | null>(null);
  const [now, setNow] = useState(() => Date.now());
  // Times and day pills are in the viewer's time zone, which only the browser knows.
  const isClient = useIsClient();

  const scroller = useRef<HTMLDivElement>(null);
  const topSentinel = useRef<HTMLDivElement>(null);
  // Scroll handling: stick to the bottom for new messages, keep the position when older ones load.
  const pendingScroll = useRef<"bottom" | { restoreFrom: number } | null>("bottom");

  const markRead = useCallback(() => {
    if (document.visibilityState === "visible") void markConversationRead(conversation.id);
  }, [conversation.id]);

  useEffect(markRead, [markRead]);
  useEffect(() => {
    document.addEventListener("visibilitychange", markRead);
    return () => document.removeEventListener("visibilitychange", markRead);
  }, [markRead]);

  useLayoutEffect(() => {
    const el = scroller.current;
    const target = pendingScroll.current;
    if (!el || !target) return;
    el.scrollTop = target === "bottom" ? el.scrollHeight : el.scrollHeight - target.restoreFrom;
    pendingScroll.current = null;
  }, [messages]);

  function isNearBottom() {
    const el = scroller.current;
    return !el || el.scrollHeight - el.scrollTop - el.clientHeight < 120;
  }

  useRealtimeEvent<Message>(RealtimeEvents.MessageReceived, (message) => {
    if (message.conversationId !== conversation.id) return;
    if (isNearBottom() || message.senderUserId === currentUserId) pendingScroll.current = "bottom";
    setMessages((current) => mergeMessages(current, [message]));
    if (message.senderUserId !== currentUserId) {
      setTypingAt(null);
      markRead();
    }
  });

  useRealtimeEvent<MessageStatusChange>(RealtimeEvents.MessageStatusChanged, (change) => {
    if (change.conversationId === conversation.id) setMessages((current) => applyStatusChange(current, change));
  });

  useRealtimeEvent<TypingEvent>(RealtimeEvents.Typing, (event) => {
    if (event.conversationId === conversation.id && event.userId === other.userId) setTypingAt(Date.now());
  });

  useRealtimeEvent<PresenceEvent>(RealtimeEvents.PresenceChanged, (event) => {
    if (event.userId === other.userId) setOtherOnline(event.online);
  });

  useEffect(() => {
    if (!connection || !connected) return;
    connection
      .invoke<string[]>("GetOnlineUsers", [other.userId])
      .then((online) => setOtherOnline(online.includes(other.userId)))
      .catch(() => setOtherOnline(false));
  }, [connection, connected, other.userId]);

  // Re-render while "typing…" is up so it disappears on time.
  useEffect(() => {
    if (typingAt === null) return;
    const timer = setInterval(() => setNow(Date.now()), 1000);
    const stop = setTimeout(() => setTypingAt(null), TYPING_DISPLAY_MS);
    return () => {
      clearInterval(timer);
      clearTimeout(stop);
    };
  }, [typingAt]);

  const loadOlder = useCallback(async () => {
    if (!hasMore || loadingOlder || messages.length === 0) return;
    setLoadingOlder(true);
    const result = await loadOlderMessages(conversation.id, messages[0].id);
    setLoadingOlder(false);
    if (result.error !== undefined) return;
    pendingScroll.current = { restoreFrom: scroller.current?.scrollHeight ?? 0 };
    setMessages((current) => mergeMessages(current, result.messages));
    setHasMore(result.hasMore);
  }, [conversation.id, hasMore, loadingOlder, messages]);

  // Infinite scroll: reaching the top loads the previous page.
  useEffect(() => {
    const sentinel = topSentinel.current;
    if (!sentinel || !hasMore) return;
    const observer = new IntersectionObserver((entries) => {
      if (entries.some((e) => e.isIntersecting)) void loadOlder();
    }, { root: scroller.current, rootMargin: "80px" });
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [hasMore, loadOlder]);

  async function handleSend(body: string, blobNames: string[]) {
    const result = await sendMessage(conversation.id, body, blobNames);
    if (result.error !== undefined) return { error: result.error };
    pendingScroll.current = "bottom";
    setMessages((current) => mergeMessages(current, [result.message]));
    return {};
  }

  const timeFormat = new Intl.DateTimeFormat(locale, { hour: "2-digit", minute: "2-digit" });
  const dayFormat = new Intl.DateTimeFormat(locale, { weekday: "long", day: "numeric", month: "long" });
  const dayPill = (iso: string) => {
    const date = new Date(iso);
    const relative = relativeDay(date, new Date(now));
    return relative ? t(relative) : dayFormat.format(date);
  };
  const typing = isTypingVisible(typingAt, now);
  const blockedByOther = thread.blockedByOther;

  return (
    <div className="flex h-full min-h-0 flex-col bg-white">
      <div className="flex items-center justify-between gap-3 border-b border-line px-5 py-3.5">
        <div className="flex min-w-0 items-center gap-3">
          {backHref && (
            <Link
              href={backHref}
              aria-label={t("backToInbox")}
              className="-ml-2 flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-ink-500 transition-colors hover:bg-bubble hover:text-ink-900 lg:hidden"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-5 w-5" aria-hidden>
                <path d="M15 5l-7 7 7 7" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
            </Link>
          )}
          <Avatar userId={other.userId} displayName={other.displayName} pictureUrl={other.avatarUrl} size={42} />
          <div className="min-w-0">
            <p className="truncate font-semibold text-ink-950">{other.displayName}</p>
            <p className="flex min-w-0 items-center gap-1.5 text-xs text-ink-500">
              <span className={cn("h-2 w-2 shrink-0 rounded-full", otherOnline ? "bg-emerald-500" : "bg-ink-300")} aria-hidden />
              <span className="shrink-0">{otherOnline ? t("online") : t("offline")}</span>
              <span aria-hidden>·</span>
              {conversation.listing.title ? (
                <Link href={`/property/${conversation.listing.id}`} className="truncate font-medium text-accent-600 hover:underline">
                  {conversation.listing.title}
                </Link>
              ) : (
                <span className="truncate">{t("listingDeleted")}</span>
              )}
            </p>
          </div>
        </div>
        <ConversationActions
          conversationId={conversation.id}
          isArchived={conversation.isArchived}
          blockedByMe={blockedByMe}
          onBlockedChange={setBlockedByMe}
        />
      </div>

      <div ref={scroller} className="flex-1 space-y-3 overflow-y-auto px-5 py-5" aria-live="polite">
        {hasMore && (
          <div ref={topSentinel} className="flex justify-center">
            <button type="button" onClick={() => void loadOlder()} className="text-xs font-medium text-ink-500 hover:text-ink-900">
              {loadingOlder ? t("loading") : t("loadOlder")}
            </button>
          </div>
        )}
        {messages.map((message, i) => (
          <div key={message.id}>
            {isClient && startsNewDay(messages[i - 1], message) && (
              <div className="my-4 flex justify-center">
                <span className="rounded-full bg-bubble px-3 py-1 text-[11px] font-medium text-ink-500">{dayPill(message.createdAt)}</span>
              </div>
            )}
            <MessageBubble
              message={message}
              mine={message.senderUserId === currentUserId}
              time={isClient ? timeFormat.format(new Date(message.createdAt)) : ""}
              statusLabel={t(`status.${message.status}`)}
            />
          </div>
        ))}
        {typing && (
          <div className="flex justify-start" role="status" aria-label={t("typingNamed", { name: other.displayName })}>
            <span className="flex items-center gap-1 rounded-[16px_16px_16px_4px] bg-bubble px-4 py-3">
              {[0, 150, 300].map((delay) => (
                <span key={delay} className="h-1.5 w-1.5 animate-typing-dot rounded-full bg-ink-400" style={{ animationDelay: `${delay}ms` }} />
              ))}
            </span>
          </div>
        )}
      </div>

      <div className="border-t border-line px-4 py-3">
        {blockedByOther ? (
          <p className="rounded-[14px] bg-bubble px-4 py-3 text-sm text-ink-600">{t("blockedByOther")}</p>
        ) : blockedByMe ? (
          <p className="rounded-[14px] bg-bubble px-4 py-3 text-sm text-ink-600">{t("blockedByMe")}</p>
        ) : (
          <MessageComposer
            onSend={handleSend}
            onTyping={() => void connection?.invoke("Typing", conversation.id).catch(() => {})}
            autoFocus
          />
        )}
      </div>
    </div>
  );
}
