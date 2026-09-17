"use server";

import { redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { clearSessionCookie, setSessionCookie } from "@/lib/auth/session";

export type AuthFormState = { error?: string };

// Only ever redirect to a same-site path — formData/query values are attacker-controlled, and an
// absolute URL here would make this an open redirect.
function safeNext(next: FormDataEntryValue | string | null): string {
  return typeof next === "string" && next.startsWith("/") ? next : "/";
}

async function completeSignIn(res: Response, next: string): Promise<AuthFormState> {
  if (!res.ok) {
    const problem = await res.json().catch(() => null);
    const message = problem?.errors
      ? Object.values(problem.errors as Record<string, string[]>).flat().join(" ")
      : ((problem?.detail as string | undefined) ?? (await getTranslations("Auth"))("genericError"));

    return { error: message };
  }

  const { token, expiresAt } = (await res.json()) as { token: string; expiresAt: string };
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

  return completeSignIn(res, safeNext(next ?? null));
}

export async function logout() {
  await clearSessionCookie();
  redirect("/");
}
