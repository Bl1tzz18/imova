"use client";

import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { Avatar } from "@/components/ui/Avatar";
import { loadOlderMessages, markConversationRead, sendMessage } from "@/lib/messaging/actions";
import { RealtimeEvents } from "@/lib/messaging/realtime";
import { applyStatusChange, isTypingVisible, mergeMessages, startsNewDay, TYPING_DISPLAY_MS } from "@/lib/messaging/thread";
import type { ConversationThread, Message, MessageStatusChange, PresenceEvent, TypingEvent } from "@/types/messaging";
import { ConversationActions } from "./ConversationActions";
import { MessageBubble } from "./MessageBubble";
import { MessageComposer } from "./MessageComposer";
import { useRealtime, useRealtimeEvent } from "./RealtimeProvider";

export function ThreadView({ thread, currentUserId }: { thread: ConversationThread; currentUserId: string }) {
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
  const typing = isTypingVisible(typingAt, now);
  const blockedByOther = thread.blockedByOther;

  return (
    <div className="flex h-[calc(100vh-12rem)] min-h-[520px] flex-col overflow-hidden rounded-2xl border border-ink-100 bg-ink-50/60">
      <div className="flex flex-wrap items-start justify-between gap-3 border-b border-ink-100 bg-white px-4 py-3">
        <div className="flex min-w-0 items-center gap-3">
          <div className="relative">
            <Avatar userId={other.userId} displayName={other.displayName} pictureUrl={other.avatarUrl} size={40} />
            {otherOnline && (
              <span className="absolute bottom-0 right-0 h-3 w-3 rounded-full border-2 border-white bg-emerald-500" aria-hidden />
            )}
          </div>
          <div className="min-w-0">
            <p className="truncate font-semibold text-ink-950">{other.displayName}</p>
            <p className="truncate text-xs text-ink-500">
              {typing ? t("typing") : otherOnline ? t("online") : t("offline")}
              {" · "}
              {conversation.listing.title ? (
                <Link href={`/property/${conversation.listing.id}`} className="text-brand-700 hover:underline">
                  {conversation.listing.title}
                </Link>
              ) : (
                t("listingDeleted")
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

      <div ref={scroller} className="flex-1 space-y-2 overflow-y-auto px-4 py-4" aria-live="polite">
        {hasMore && (
          <div ref={topSentinel} className="flex justify-center py-2">
            <button type="button" onClick={() => void loadOlder()} className="text-xs font-medium text-brand-700 hover:underline">
              {loadingOlder ? t("loading") : t("loadOlder")}
            </button>
          </div>
        )}
        {messages.map((message, i) => (
          <div key={message.id}>
            {startsNewDay(messages[i - 1], message) && (
              <p className="my-3 text-center text-[11px] font-medium uppercase tracking-wide text-ink-400">
                {dayFormat.format(new Date(message.createdAt))}
              </p>
            )}
            <MessageBubble
              message={message}
              mine={message.senderUserId === currentUserId}
              time={timeFormat.format(new Date(message.createdAt))}
              statusLabel={t(`status.${message.status}`)}
            />
          </div>
        ))}
        {typing && <p className="text-xs italic text-ink-500">{t("typingNamed", { name: other.displayName })}</p>}
      </div>

      <div className="border-t border-ink-100 bg-white p-3">
        {blockedByOther ? (
          <p className="rounded-xl bg-ink-100 px-4 py-3 text-sm text-ink-600">{t("blockedByOther")}</p>
        ) : blockedByMe ? (
          <p className="rounded-xl bg-ink-100 px-4 py-3 text-sm text-ink-600">{t("blockedByMe")}</p>
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
