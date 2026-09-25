import type { Message, MessageStatus, MessageStatusChange } from "@/types/messaging";

// Pure helpers for the thread view — kept here (not in the components) so they're unit-tested.

export const MAX_MESSAGE_LENGTH = 2000;
export const MAX_ATTACHMENTS = 5;

// How long a "typing…" indicator stays up without a fresh event, and how often we send one.
export const TYPING_DISPLAY_MS = 5000;
export const TYPING_SEND_INTERVAL_MS = 3000;

const STATUS_RANK: Record<MessageStatus, number> = { Sent: 0, Delivered: 1, Read: 2 };

function byTime(a: Message, b: Message) {
  return a.createdAt === b.createdAt ? a.id.localeCompare(b.id) : a.createdAt.localeCompare(b.createdAt);
}

// Adds messages (a live push, an older page, our own just-sent one) without duplicates — the same
// message can arrive both from the send response and over the realtime channel — keeping the
// most advanced status of the two copies. Result is oldest first.
export function mergeMessages(existing: readonly Message[], incoming: readonly Message[]): Message[] {
  const byId = new Map(existing.map((m) => [m.id, m]));
  for (const message of incoming) {
    const current = byId.get(message.id);
    byId.set(
      message.id,
      current && STATUS_RANK[current.status] > STATUS_RANK[message.status] ? { ...message, status: current.status } : message,
    );
  }
  return [...byId.values()].sort(byTime);
}

// Delivered/Read receipts only ever move a status forward (a late "Delivered" never undoes "Read").
export function applyStatusChange(messages: readonly Message[], change: MessageStatusChange): Message[] {
  const ids = new Set(change.messageIds);
  return messages.map((m) =>
    ids.has(m.id) && STATUS_RANK[change.status] > STATUS_RANK[m.status] ? { ...m, status: change.status } : m,
  );
}

// Throttles outgoing "I'm typing" events.
export function shouldSendTyping(lastSentAt: number | null, now: number): boolean {
  return lastSentAt === null || now - lastSentAt >= TYPING_SEND_INTERVAL_MS;
}

export function isTypingVisible(lastTypingAt: number | null, now: number): boolean {
  return lastTypingAt !== null && now - lastTypingAt < TYPING_DISPLAY_MS;
}

// Header badge text.
export function unreadBadgeLabel(count: number): string | null {
  if (count <= 0) return null;
  return count > 99 ? "99+" : String(count);
}

// Inbox preview line: the text (shortened), or a placeholder for an image-only message.
export function messagePreview(message: Message | null, imageLabel: string, maxLength = 80): string {
  if (!message) return "";
  const text = message.body.trim().replace(/\s+/g, " ");
  if (!text) return message.attachments.length > 0 ? imageLabel : "";
  return text.length > maxLength ? `${text.slice(0, maxLength - 1)}…` : text;
}

// Whether a message should render as a new day group ("Azi", "25 sept.") after the previous one.
export function startsNewDay(previous: Message | undefined, message: Message): boolean {
  return !previous || new Date(previous.createdAt).toDateString() !== new Date(message.createdAt).toDateString();
}
