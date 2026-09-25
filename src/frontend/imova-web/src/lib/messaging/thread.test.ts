import { describe, expect, it } from "vitest";
import {
  applyStatusChange,
  isTypingVisible,
  mergeMessages,
  messagePreview,
  shouldSendTyping,
  startsNewDay,
  unreadBadgeLabel,
} from "@/lib/messaging/thread";
import type { Message } from "@/types/messaging";

const msg = (id: string, createdAt: string, extra: Partial<Message> = {}): Message => ({
  id,
  conversationId: "c1",
  senderUserId: "u1",
  body: `Mesaj ${id}`,
  attachments: [],
  createdAt,
  status: "Sent",
  ...extra,
});

describe("mergeMessages", () => {
  it("adds older pages and live messages in time order, without duplicates", () => {
    const current = [msg("b", "2026-09-25T10:01:00Z"), msg("c", "2026-09-25T10:02:00Z")];
    const merged = mergeMessages(current, [msg("a", "2026-09-25T10:00:00Z"), msg("c", "2026-09-25T10:02:00Z"), msg("d", "2026-09-25T10:03:00Z")]);

    expect(merged.map((m) => m.id)).toEqual(["a", "b", "c", "d"]);
  });

  it("keeps the more advanced status when the same message arrives twice", () => {
    const read = msg("a", "2026-09-25T10:00:00Z", { status: "Read" });
    expect(mergeMessages([read], [msg("a", "2026-09-25T10:00:00Z")])[0].status).toBe("Read");
    expect(mergeMessages([msg("a", "2026-09-25T10:00:00Z")], [read])[0].status).toBe("Read");
  });
});

describe("applyStatusChange", () => {
  const messages = [msg("a", "2026-09-25T10:00:00Z"), msg("b", "2026-09-25T10:01:00Z", { status: "Read" })];

  it("moves the listed messages forward: Sent → Delivered → Read", () => {
    const delivered = applyStatusChange(messages, { conversationId: "c1", messageIds: ["a"], status: "Delivered" });
    expect(delivered.map((m) => m.status)).toEqual(["Delivered", "Read"]);

    const read = applyStatusChange(delivered, { conversationId: "c1", messageIds: ["a"], status: "Read" });
    expect(read[0].status).toBe("Read");
  });

  it("never moves a status back", () => {
    const late = applyStatusChange(messages, { conversationId: "c1", messageIds: ["b"], status: "Delivered" });
    expect(late[1].status).toBe("Read");
  });
});

describe("typing indicator", () => {
  it("sends at most one typing event every 3 seconds", () => {
    expect(shouldSendTyping(null, 1000)).toBe(true);
    expect(shouldSendTyping(1000, 3999)).toBe(false);
    expect(shouldSendTyping(1000, 4000)).toBe(true);
  });

  it("shows 'typing…' for 5 seconds after the last event", () => {
    expect(isTypingVisible(null, 1000)).toBe(false);
    expect(isTypingVisible(1000, 5999)).toBe(true);
    expect(isTypingVisible(1000, 6000)).toBe(false);
  });
});

describe("unreadBadgeLabel", () => {
  it.each([
    [0, null],
    [-1, null],
    [3, "3"],
    [99, "99"],
    [100, "99+"],
  ])("%i → %j", (count, label) => {
    expect(unreadBadgeLabel(count)).toBe(label);
  });
});

describe("messagePreview", () => {
  it("shortens long text and collapses whitespace", () => {
    expect(messagePreview(msg("a", "x", { body: "Bună   ziua,\n mai este?" }), "[img]")).toBe("Bună ziua, mai este?");
    expect(messagePreview(msg("a", "x", { body: "a".repeat(100) }), "[img]", 10)).toBe(`${"a".repeat(9)}…`);
  });

  it("uses the image label for an image-only message", () => {
    expect(messagePreview(msg("a", "x", { body: "", attachments: [{ id: "1", url: "u", contentType: "image/png" }] }), "📷 Imagine")).toBe(
      "📷 Imagine",
    );
    expect(messagePreview(null, "[img]")).toBe("");
  });
});

describe("startsNewDay", () => {
  it("groups by calendar day", () => {
    const morning = msg("a", "2026-09-25T08:00:00");
    expect(startsNewDay(undefined, morning)).toBe(true);
    expect(startsNewDay(morning, msg("b", "2026-09-25T18:00:00"))).toBe(false);
    expect(startsNewDay(morning, msg("c", "2026-09-26T08:00:00"))).toBe(true);
  });
});
