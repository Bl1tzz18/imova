import { getSessionToken } from "@/lib/auth/session";
import type {
  AdminConversation,
  ConversationSummary,
  ConversationThread,
  FlaggedMessage,
  MessagingReport,
} from "@/types/messaging";

// Server-side reads for Server Components (the session token never leaves the server). Every
// function returns null when logged out or when the API says not found / not allowed.

async function apiGet<T>(path: string): Promise<T | null> {
  const token = await getSessionToken();
  if (!token) return null;

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}${path}`, { headers: { Authorization: `Bearer ${token}` }, cache: "no-store" });
  if (!res.ok) return null;
  return (await res.json()) as T;
}

export function getConversations(search?: string, archived = false) {
  const params = new URLSearchParams();
  if (search) params.set("search", search);
  if (archived) params.set("archived", "true");
  return apiGet<ConversationSummary[]>(`/api/v1/messaging/conversations?${params}`);
}

export function getThread(conversationId: string) {
  return apiGet<ConversationThread>(`/api/v1/messaging/conversations/${conversationId}`);
}

export async function getConversationIdForListing(listingId: string): Promise<string | null> {
  const found = await apiGet<{ conversationId: string }>(`/api/v1/messaging/conversations/by-listing/${listingId}`);
  return found?.conversationId ?? null;
}

export async function getUnreadCount(): Promise<number> {
  return (await apiGet<{ count: number }>("/api/v1/messaging/unread-count"))?.count ?? 0;
}

export function getMessagingReports(includeResolved = false) {
  return apiGet<MessagingReport[]>(`/api/v1/admin/messaging/reports${includeResolved ? "?includeResolved=true" : ""}`);
}

export function getFlaggedMessages() {
  return apiGet<FlaggedMessage[]>("/api/v1/admin/messaging/flagged-messages");
}

export function getAdminConversation(conversationId: string) {
  return apiGet<AdminConversation>(`/api/v1/admin/messaging/conversations/${conversationId}`);
}
