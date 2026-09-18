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

export async function logout() {
  await clearSessionCookie();
  redirect("/");
}
