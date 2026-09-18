"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { getTranslations } from "next-intl/server";
import { clearSessionCookie, getSessionToken, setSessionCookie } from "@/lib/auth/session";

export type AuthFormState = { error?: string; success?: boolean };

// Only ever redirect to a same-site path — formData/query values are attacker-controlled, and an
// absolute URL here would make this an open redirect.
function safeNext(next: FormDataEntryValue | string | null): string {
  return typeof next === "string" && next.startsWith("/") ? next : "/";
}

type AuthResponseBody = {
  token: string;
  expiresAt: string;
  user: { requiresPhoneNumber: boolean };
};

async function readAuthError(res: Response): Promise<AuthFormState> {
  const problem = await res.json().catch(() => null);
  const message = problem?.errors
    ? Object.values(problem.errors as Record<string, string[]>).flat().join(" ")
    : ((problem?.detail as string | undefined) ?? (await getTranslations("Auth"))("genericError"));

  return { error: message };
}

async function completeSignIn(res: Response, next: string): Promise<AuthFormState> {
  if (!res.ok) {
    return readAuthError(res);
  }

  const { token, expiresAt } = (await res.json()) as AuthResponseBody;
  await setSessionCookie(token, expiresAt);
  redirect(next);
}

export async function login(_prevState: AuthFormState, formData: FormData): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const res = await fetch(`${apiUrl}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      email: formData.get("email"),
      password: formData.get("password"),
    }),
  });

  return completeSignIn(res, safeNext(formData.get("next")));
}

export async function register(_prevState: AuthFormState, formData: FormData): Promise<AuthFormState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const res = await fetch(`${apiUrl}/api/v1/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
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
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ idToken }),
  });

  if (!res.ok) {
    return readAuthError(res);
  }

  const { token, expiresAt, user } = (await res.json()) as AuthResponseBody;
  await setSessionCookie(token, expiresAt);

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

  revalidatePath("/account");
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

export async function logout() {
  await clearSessionCookie();
  redirect("/");
}
