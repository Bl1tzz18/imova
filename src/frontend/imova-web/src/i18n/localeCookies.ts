// Server only: called from the auth and locale server actions (cookies() fails anywhere else).
import { cookies } from "next/headers";
import { localeCookieName, type Locale } from "./config";
import { localeAfterSignIn, localeChosenCookieName } from "./signInLocale";

export async function writeLocaleCookie(locale: Locale) {
  const cookieStore = await cookies();
  cookieStore.set(localeCookieName, locale, { path: "/", maxAge: 60 * 60 * 24 * 365, sameSite: "lax" });
}

// PUT /users/me/language. Best effort: the site's language changes either way.
export async function saveAccountLanguage(locale: Locale, token: string) {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  try {
    await fetch(`${apiUrl}/api/v1/users/me/language`, {
      method: "PUT",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify({ language: locale }),
    });
  } catch {
    // The API being unreachable must not stop the sign-in or the switch; the next switch saves it.
  }
}

// Right after a sign-in (password, Google or registration): the site switches to the account's
// language, unless the visitor chose one on this device while signed out — then that one stays
// and the account follows it. The choice is used up either way.
export async function applySignInLocale(accountLanguage: string | undefined, token: string) {
  const cookieStore = await cookies();
  const chosen = cookieStore.get(localeChosenCookieName)
    ? cookieStore.get(localeCookieName)?.value
    : undefined;
  const { locale, saveToAccount } = localeAfterSignIn(accountLanguage, chosen);

  await writeLocaleCookie(locale);
  cookieStore.delete(localeChosenCookieName);
  if (saveToAccount) {
    await saveAccountLanguage(locale, token);
  }
}
