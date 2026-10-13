"use server";

import { cookies } from "next/headers";
import { getSessionToken } from "@/lib/auth/session";
import { isLocale, localeCookieName } from "./config";

export async function setLocale(locale: string) {
  if (!isLocale(locale)) return;

  const cookieStore = await cookies();
  cookieStore.set(localeCookieName, locale, {
    path: "/",
    maxAge: 60 * 60 * 24 * 365,
    sameSite: "lax",
  });

  // Signed in: the account follows the site's language — its emails are written in it. There's no
  // separate setting. Best effort: the site switches language either way.
  const token = await getSessionToken();
  if (!token) return;
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  try {
    await fetch(`${apiUrl}/api/v1/users/me/language`, {
      method: "PUT",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify({ language: locale }),
    });
  } catch {
    // The API being unreachable must not stop the switch; the next switch saves it.
  }
}
