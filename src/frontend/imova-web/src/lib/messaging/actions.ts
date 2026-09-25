"use server";

import { revalidatePath } from "next/cache";
import { getTranslations } from "next-intl/server";
import { getSessionToken } from "@/lib/auth/session";
import type { Message, ReportReason } from "@/types/messaging";

// Messaging calls made from client components. They run on the server so the session token (an
// httpOnly cookie) is never exposed to browser code.

type ActionResult<T = object> = ({ error?: undefined } & T) | { error: string };

async function call(path: string, init: RequestInit = {}): Promise<Response | null> {
  const token = await getSessionToken();
  if (!token) return null;
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  return fetch(`${apiUrl}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}`, ...init.headers },
    cache: "no-store",
  });
}

// The API's own message (validation errors, "blocked", rate limit, ...) when it has one.
async function errorFrom(res: Response | null): Promise<{ error: string }> {
  const t = await getTranslations("Messages");
  if (!res) return { error: t("loginRequired") };
  const problem = await res.json().catch(() => null);
  const message = problem?.errors
    ? Object.values(problem.errors as Record<string, string[]>).flat().join(" ")
    : (problem?.detail as string | undefined);
  return { error: message || t("genericError") };
}

export async function startConversation(
  listingId: string,
  body: string,
  attachmentBlobNames: string[],
): Promise<ActionResult<{ conversationId: string }>> {
  const res = await call("/api/v1/messaging/conversations", {
    method: "POST",
    body: JSON.stringify({ listingId, body, attachmentBlobNames }),
  });
  if (!res?.ok) return errorFrom(res);
  const result = (await res.json()) as { conversationId: string };
  revalidatePath("/messages");
  return { conversationId: result.conversationId };
}

export async function sendMessage(
  conversationId: string,
  body: string,
  attachmentBlobNames: string[],
): Promise<ActionResult<{ message: Message }>> {
  const res = await call(`/api/v1/messaging/conversations/${conversationId}/messages`, {
    method: "POST",
    body: JSON.stringify({ body, attachmentBlobNames }),
  });
  if (!res?.ok) return errorFrom(res);
  return { message: (await res.json()) as Message };
}

export async function loadOlderMessages(
  conversationId: string,
  before: string,
): Promise<ActionResult<{ messages: Message[]; hasMore: boolean }>> {
  const res = await call(`/api/v1/messaging/conversations/${conversationId}?before=${encodeURIComponent(before)}`);
  if (!res?.ok) return errorFrom(res);
  const thread = (await res.json()) as { messages: Message[]; hasMore: boolean };
  return { messages: thread.messages, hasMore: thread.hasMore };
}

export async function markConversationRead(conversationId: string): Promise<void> {
  await call(`/api/v1/messaging/conversations/${conversationId}/read`, { method: "POST" });
}

async function post(path: string, body?: unknown): Promise<ActionResult> {
  const res = await call(path, { method: "POST", body: body === undefined ? undefined : JSON.stringify(body) });
  if (!res?.ok) return errorFrom(res);
  revalidatePath("/messages");
  return {};
}

export async function setConversationArchived(conversationId: string, archived: boolean) {
  return post(`/api/v1/messaging/conversations/${conversationId}/${archived ? "archive" : "unarchive"}`);
}

export async function setUserBlocked(conversationId: string, blocked: boolean) {
  return post(`/api/v1/messaging/conversations/${conversationId}/${blocked ? "block" : "unblock"}`);
}

export async function reportConversation(conversationId: string, reason: ReportReason, details: string) {
  return post(`/api/v1/messaging/conversations/${conversationId}/report`, { reason, details: details.trim() || null });
}

export async function requestAttachmentUploadUrl(fileExtension: string): Promise<ActionResult<{ uploadUrl: string; blobName: string }>> {
  const res = await call("/api/v1/messaging/attachments/upload-url", { method: "POST", body: JSON.stringify({ fileExtension }) });
  if (!res?.ok) return errorFrom(res);
  const result = (await res.json()) as { uploadUrl: string; blobName: string };
  return { uploadUrl: result.uploadUrl, blobName: result.blobName };
}

// A short-lived token only the realtime hub accepts (the browser never gets the session token).
export async function getRealtimeToken(): Promise<string | null> {
  const res = await call("/api/v1/messaging/realtime-token", { method: "POST" });
  if (!res?.ok) return null;
  return ((await res.json()) as { token: string }).token;
}

// --- Admin ---

export async function resolveMessagingReport(reportId: string) {
  const result = await post(`/api/v1/admin/messaging/reports/${reportId}/resolve`);
  revalidatePath("/admin/messaging");
  return result;
}

export async function setMessagingBan(userId: string, banned: boolean) {
  const result = await post(`/api/v1/admin/messaging/users/${userId}/${banned ? "ban" : "unban"}`);
  revalidatePath("/admin/messaging");
  return result;
}
