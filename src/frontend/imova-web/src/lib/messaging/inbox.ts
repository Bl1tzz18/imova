import type { ConversationSummary, Message } from "@/types/messaging";

// A live message for an inbox already on screen: the conversation moves to the top with the new
// preview, and — if someone else sent it and it isn't the conversation being viewed — its unread
// count goes up. Returns null when the conversation isn't in the list (the caller refetches).
export function applyIncomingMessage(
  conversations: readonly ConversationSummary[],
  message: Message,
  currentUserId: string,
  openConversationId: string | null = null,
): ConversationSummary[] | null {
  const index = conversations.findIndex((c) => c.id === message.conversationId);
  if (index === -1) return null;

  const current = conversations[index];
  if (current.lastMessage?.id === message.id) return [...conversations];

  const fromOther = message.senderUserId !== currentUserId;
  const updated: ConversationSummary = {
    ...current,
    lastMessage: message,
    lastMessageAt: message.createdAt,
    unreadCount: current.unreadCount + (fromOther && message.conversationId !== openConversationId ? 1 : 0),
  };
  return [updated, ...conversations.filter((_, i) => i !== index)];
}

// The conversation was opened (and marked read).
export function markConversationRead(conversations: readonly ConversationSummary[], conversationId: string): ConversationSummary[] {
  return conversations.map((c) => (c.id === conversationId ? { ...c, unreadCount: 0 } : c));
}
