import { describe, expect, it } from "vitest";
import { applyIncomingMessage, markConversationRead } from "@/lib/messaging/inbox";
import type { ConversationSummary, Message } from "@/types/messaging";

const conversation = (id: string, unreadCount = 0): ConversationSummary => ({
  id,
  listing: { id: `l-${id}`, title: `Anunț ${id}`, photoUrl: null },
  otherParticipant: { userId: `u-${id}`, displayName: `User ${id}`, avatarUrl: null },
  lastMessage: null,
  unreadCount,
  lastMessageAt: "2026-09-25T09:00:00Z",
  isArchived: false,
  isInitiator: true,
});

const incoming = (conversationId: string, senderUserId: string, id = "m1"): Message => ({
  id,
  conversationId,
  senderUserId,
  body: "Salut",
  attachments: [],
  createdAt: "2026-09-25T10:00:00Z",
  status: "Sent",
});

describe("applyIncomingMessage", () => {
  const inbox = [conversation("a"), conversation("b", 2)];

  it("moves the conversation to the top and counts it as unread when someone else sent it", () => {
    const updated = applyIncomingMessage(inbox, incoming("b", "u-b"), "me")!;

    expect(updated.map((c) => c.id)).toEqual(["b", "a"]);
    expect(updated[0].unreadCount).toBe(3);
    expect(updated[0].lastMessage?.body).toBe("Salut");
    expect(updated[0].lastMessageAt).toBe("2026-09-25T10:00:00Z");
  });

  it("doesn't count my own messages or the conversation I'm reading", () => {
    expect(applyIncomingMessage(inbox, incoming("b", "me"), "me")![0].unreadCount).toBe(2);
    expect(applyIncomingMessage(inbox, incoming("b", "u-b"), "me", "b")![0].unreadCount).toBe(2);
  });

  it("ignores the same message twice", () => {
    const once = applyIncomingMessage(inbox, incoming("b", "u-b"), "me")!;
    expect(applyIncomingMessage(once, incoming("b", "u-b"), "me")![0].unreadCount).toBe(3);
  });

  it("returns null for a conversation that isn't listed yet (the caller refetches)", () => {
    expect(applyIncomingMessage(inbox, incoming("new", "u-x"), "me")).toBeNull();
  });
});

describe("markConversationRead", () => {
  it("clears that conversation's unread count only", () => {
    const updated = markConversationRead([conversation("a", 4), conversation("b", 2)], "a");
    expect(updated.map((c) => c.unreadCount)).toEqual([0, 2]);
  });
});
