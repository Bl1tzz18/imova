"use server";

import { redirect } from "next/navigation";
import { apiErrorMessage } from "@/lib/api/errorMessage";
import { forwardedForHeader } from "@/lib/auth/clientIp";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { clearSessionCookies, getSessionToken } from "@/lib/auth/session";
import type { EmailPreferences } from "@/lib/account/emailPreferences";

export type AccountDeletionState = { error?: string; linkSent?: boolean };

// Deletes the signed-in account, confirmed with its password (the API checks it and applies the
// sign-in lockout). The account's sessions die with it; the cookies go here, then a goodbye page.
export async function deleteAccount(_prevState: AccountDeletionState, formData: FormData): Promise<AccountDeletionState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/account?tab=privacy");
  }

  const res = await fetch(`${apiUrl}/api/v1/users/me`, {
    method: "DELETE",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify({ password: formData.get("password") || null }),
  });

  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  await clearSessionCookies();
  redirect("/account-deleted");
}

// For an account without a password (Google sign-up): it confirms from the emailed link instead.
export async function requestAccountDeletionLink(_prevState: AccountDeletionState): Promise<AccountDeletionState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/account?tab=privacy");
  }

  const res = await fetch(`${apiUrl}/api/v1/users/me/deletion-link`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  return { linkSent: true };
}

// The final button on /delete-account (the emailed link). Never called while rendering the page —
// mail scanners open links, and opening one must not delete anything.
export async function confirmAccountDeletion(_prevState: AccountDeletionState, formData: FormData): Promise<AccountDeletionState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const userId = formData.get("userId");
  const signedIn = await getCurrentUserProfile();

  const res = await fetch(`${apiUrl}/api/v1/auth/delete-account`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...(await forwardedForHeader()) },
    body: JSON.stringify({ userId, token: formData.get("token") }),
  });

  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  // Only when this browser is signed in to the account that was just deleted — the link may have
  // been opened where someone else is signed in.
  if (signedIn?.id === userId) {
    await clearSessionCookies();
  }

  redirect("/account-deleted");
}

// The Notifications tab's switches: saves right away, returns what the API stored.
export async function updateEmailPreferences(
  preferences: EmailPreferences,
): Promise<{ preferences: EmailPreferences } | { error: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/account?tab=notifications");
  }

  const res = await fetch(`${apiUrl}/api/v1/users/me/email-preferences`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify(preferences),
  });

  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  return { preferences: (await res.json()) as EmailPreferences };
}
