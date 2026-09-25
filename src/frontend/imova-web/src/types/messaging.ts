// Mirrors Imova.Contracts.Messaging.

export type MessageStatus = "Sent" | "Delivered" | "Read";

// url is the API path; the browser loads it through the site proxy (see attachmentSrc).
export type MessageAttachment = { id: string; url: string; contentType: string };

export type Message = {
  id: string;
  conversationId: string;
  senderUserId: string;
  body: string;
  attachments: MessageAttachment[];
  createdAt: string;
  status: MessageStatus;
};

export type ConversationParticipant = { userId: string; displayName: string; avatarUrl: string | null };

// title is null when the listing has since been deleted.
export type ConversationListing = { id: string; title: string | null; photoUrl: string | null };

export type ConversationSummary = {
  id: string;
  listing: ConversationListing;
  otherParticipant: ConversationParticipant;
  lastMessage: Message | null;
  unreadCount: number;
  lastMessageAt: string;
  isArchived: boolean;
  // The viewer started it (they're the visitor, not the listing's publisher).
  isInitiator: boolean;
};

export type ConversationThread = {
  conversation: ConversationSummary;
  // Oldest first; hasMore = older messages exist.
  messages: Message[];
  hasMore: boolean;
  blockedByMe: boolean;
  blockedByOther: boolean;
};

export type MessageStatusChange = { conversationId: string; messageIds: string[]; status: MessageStatus };

export type TypingEvent = { conversationId: string; userId: string };

export type PresenceEvent = { userId: string; online: boolean };

export type ReportReason = "Spam" | "Fraud" | "Abuse" | "Other";

export type MessagingUser = { id: string; displayName: string | null; email: string | null; isBannedFromMessaging: boolean };

export type MessagingReport = {
  id: string;
  conversationId: string;
  reason: ReportReason;
  details: string | null;
  createdAt: string;
  resolvedAt: string | null;
  reporter: MessagingUser;
  reportedUser: MessagingUser;
  listing: ConversationListing;
  // The admin who resolved it; null while active.
  resolvedBy: MessagingUser | null;
};

export type FlaggedMessage = {
  message: Message;
  flagReason: string;
  sender: MessagingUser;
  resolvedAt: string | null;
  resolvedBy: MessagingUser | null;
};

export type AdminConversation = {
  id: string;
  listing: ConversationListing;
  initiator: MessagingUser;
  publisher: MessagingUser;
  messages: Message[];
};
