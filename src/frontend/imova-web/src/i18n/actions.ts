"use server";

import { cookies } from "next/headers";
import { getSessionToken } from "@/lib/auth/session";
import { isLocale } from "./config";
import { localeChosenCookieName } from "./signInLocale";
import { saveAccountLanguage, writeLocaleCookie } from "./localeCookies";

export async function setLocale(locale: string) {
  if (!isLocale(locale)) return;

  await writeLocaleCookie(locale);

  const cookieStore = await cookies();
  const token = await getSessionToken();
  if (!token) {
    // Signed out: remember that this device's language was picked on purpose, so the next
    // sign-in keeps it (see localeAfterSignIn) instead of switching to the account's.
    cookieStore.set(localeChosenCookieName, "1", { path: "/", maxAge: 60 * 60 * 24 * 365, sameSite: "lax" });
    return;
  }

  // Signed in: the account follows the site's language — its emails are written in it. There's no
  // separate setting.
  await saveAccountLanguage(locale, token);
}
