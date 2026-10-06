"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { forwardedForHeader } from "@/lib/auth/clientIp";
import { clearSessionCookies, getRefreshToken, getSessionToken, setSessionCookies } from "@/lib/auth/session";
import type { SessionTokens } from "@/lib/auth/sessionCookies";
import { apiErrorMessage } from "@/lib/api/errorMessage";

export type AuthFormState = { error?: string; success?: boolean };

// Only ever redirect to a same-site path — formData/query values are attacker-controlled, and an
// absolute URL here would make this an open redirect.
function safeNext(next: FormDataEntryValue | string | null): string {
  return typeof next === "string" && next.startsWith("/") ? next : "/";
}

type AuthResponseBody = SessionTokens & {
  user: { requiresPhoneNumber: boolean };
};

// Translated into the current UI language from the API's error code (see lib/api/problem.ts).
async function readAuthError(res: Response): Promise<AuthFormState> {
  return { error: await apiErrorMessage(res) };
}

async function completeSignIn(res: Response, next: string): Promise<AuthFormState> {
  if (!res.ok) {
    return readAuthError(res);
  }

  await setSessionCookies((await res.json()) as AuthResponseBody);
  redirect(next);
}

export async function login(_prevState: AuthFormState, formData: FormData): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const res = await fetch(`${apiUrl}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...(await forwardedForHeader()) },
    body: JSON.stringify({
      email: formData.get("email"),
      password: formData.get("password"),
      // "Ține-mă minte": a 30-day session instead of one that ends with the browser.
      rememberMe: formData.get("rememberMe") === "on",
    }),
  });

  return completeSignIn(res, safeNext(formData.get("next")));
}

export async function register(_prevState: AuthFormState, formData: FormData): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const res = await fetch(`${apiUrl}/api/v1/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...(await forwardedForHeader()) },
    body: JSON.stringify({
      email: formData.get("email"),
      password: formData.get("password"),
      displayName: formData.get("name"),
      phoneNumber: formData.get("phone"),
    }),
  });

  return completeSignIn(res, safeNext(formData.get("next")));
}

export async function googleLogin(idToken: string, next?: string): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const res = await fetch(`${apiUrl}/api/v1/auth/google`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...(await forwardedForHeader()) },
    body: JSON.stringify({ idToken }),
  });

  if (!res.ok) {
    return readAuthError(res);
  }

  const session = (await res.json()) as AuthResponseBody;
  await setSessionCookies(session);
  const { user } = session;

  const safeNextPath = safeNext(next ?? null);
  redirect(
    user.requiresPhoneNumber ? `/complete-profile?next=${encodeURIComponent(safeNextPath)}` : safeNextPath,
  );
}

export async function completePhoneNumber(
  _prevState: AuthFormState,
  formData: FormData,
): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login");
  }

  const res = await fetch(`${apiUrl}/api/v1/auth/phone`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify({ phoneNumber: formData.get("phone") }),
  });

  if (!res.ok) {
    return readAuthError(res);
  }

  redirect(safeNext(formData.get("next")));
}

export async function updateProfile(_prevState: AuthFormState, formData: FormData): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login");
  }

  const res = await fetch(`${apiUrl}/api/v1/auth/profile`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify({
      displayName: formData.get("name"),
      phoneNumber: formData.get("phone"),
    }),
  });

  if (!res.ok) {
    return readAuthError(res);
  }

  revalidatePath("/account");
  return { success: true };
}

export async function changePassword(_prevState: AuthFormState, formData: FormData): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login");
  }

  const res = await fetch(`${apiUrl}/api/v1/auth/password`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify({
      currentPassword: formData.get("currentPassword") || null,
      newPassword: formData.get("newPassword"),
    }),
  });

  if (!res.ok) {
    return readAuthError(res);
  }

  // A new password signs out every session, this one included — keep this one with the new tokens.
  const { session } = (await res.json()) as { session: SessionTokens };
  await setSessionCookies(session);

  revalidatePath("/account");
  return { success: true };
}

// "Sign out on every other device": the API revokes every session and returns new tokens for this one.
export async function signOutOtherSessions(_prevState: AuthFormState): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login");
  }

  const res = await fetch(`${apiUrl}/api/v1/auth/sign-out-other-sessions`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    return readAuthError(res);
  }

  await setSessionCookies((await res.json()) as SessionTokens);
  return { success: true };
}

// Always "sent" as far as the user can tell — the API answers the same whether or not the email
// has an account, so the page can't reveal that either.
export async function forgotPassword(_prevState: AuthFormState, formData: FormData): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const res = await fetch(`${apiUrl}/api/v1/auth/forgot-password`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...(await forwardedForHeader()) },
    body: JSON.stringify({ email: formData.get("email") }),
  });

  if (!res.ok) {
    return readAuthError(res);
  }

  return { success: true };
}

// From the emailed link (/reset-password?email=…&token=…). On success the user signs in with the
// new password — /login shows a "password changed" notice.
export async function resetPassword(_prevState: AuthFormState, formData: FormData): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const res = await fetch(`${apiUrl}/api/v1/auth/reset-password`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...(await forwardedForHeader()) },
    body: JSON.stringify({
      email: formData.get("email"),
      token: formData.get("token"),
      newPassword: formData.get("newPassword"),
    }),
  });

  if (!res.ok) {
    return readAuthError(res);
  }

  redirect("/login?reset=1");
}

// Called while rendering /confirm-email (the emailed link) rather than from a form: opening the
// link is the confirmation. Harmless to repeat — an already-confirmed account just succeeds.
export async function confirmEmail(userId: string, token: string): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const res = await fetch(`${apiUrl}/api/v1/auth/confirm-email`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...(await forwardedForHeader()) },
    body: JSON.stringify({ userId, token }),
    cache: "no-store",
  });

  if (!res.ok) {
    return readAuthError(res);
  }

  // No revalidatePath here: it isn't allowed during a render, and the pages it would refresh
  // (my listings, account) fetch with cache: "no-store" anyway.
  return { success: true };
}

export async function resendEmailConfirmation(): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login");
  }

  const res = await fetch(`${apiUrl}/api/v1/auth/resend-confirmation`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}`, ...(await forwardedForHeader()) },
  });

  if (!res.ok) {
    return readAuthError(res);
  }

  return { success: true };
}

export type UploadProfilePictureResult = { error?: string; profilePictureUrl?: string | null };

// Called directly from the client (not via useActionState) — this is an immediate
// upload-on-file-select interaction, not a <form> submission, same pattern as googleLogin()
// above. Proxied through a Server Action (rather than the browser calling the API directly, the
// way listing-photo uploads do in lib/api/media.ts) because this endpoint is authenticated and
// the JWT lives in an httpOnly cookie client-side JS can't read.
export async function uploadProfilePicture(file: File): Promise<UploadProfilePictureResult> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login");
  }

  const uploadForm = new FormData();
  uploadForm.append("file", file);

  const res = await fetch(`${apiUrl}/api/v1/users/me/profile-picture`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: uploadForm,
  });

  if (!res.ok) {
    const { error } = await readAuthError(res);
    return { error };
  }

  const profile = (await res.json()) as { profilePictureUrl: string | null };
  revalidatePath("/account");
  return { profilePictureUrl: profile.profilePictureUrl };
}

// Same proxy-through-a-Server-Action reasoning as uploadProfilePicture above.
export async function removeProfilePicture(): Promise<UploadProfilePictureResult> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login");
  }

  const res = await fetch(`${apiUrl}/api/v1/users/me/profile-picture`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    const { error } = await readAuthError(res);
    return { error };
  }

  const profile = (await res.json()) as { profilePictureUrl: string | null };
  revalidatePath("/account");
  return { profilePictureUrl: profile.profilePictureUrl };
}

// Ends the session on the server too (its refresh token stops working), then drops the cookies.
// Best effort: the cookies go either way.
export async function logout() {
  await endSession();
  redirect("/");
}

// Signs out, then straight to the sign-in page, which comes back to `next` (a path on this site) —
// "switch accounts", e.g. on an invitation sent to another address.
export async function logoutTo(next: string) {
  await endSession();
  const safeNext = next.startsWith("/") && !next.startsWith("//") ? next : "/";
  redirect(`/login?next=${encodeURIComponent(safeNext)}`);
}

async function endSession() {
  const refreshToken = await getRefreshToken();
  if (refreshToken) {
    const apiUrl = process.env.API_URL ?? "http://localhost:8080";
    try {
      await fetch(`${apiUrl}/api/v1/auth/logout`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken }),
      });
    } catch {
      // The API being down must not keep anyone signed in on this browser.
    }
  }

  await clearSessionCookies();
}
