import { cookies } from "next/headers";
import {
  REFRESH_COOKIE,
  SESSION_COOKIE,
  sessionCookies,
  type SessionTokens,
} from "@/lib/auth/sessionCookies";

// httpOnly so the tokens are never reachable from client-side JS (XSS-safe) — only Server
// Components/Actions (and src/middleware.ts, which renews the login token) can read them.
export async function setSessionCookies(session: SessionTokens) {
  const cookieStore = await cookies();
  for (const { name, value, options } of sessionCookies(session, process.env.NODE_ENV === "production")) {
    cookieStore.set(name, value, options);
  }
}

export async function clearSessionCookies() {
  const cookieStore = await cookies();
  cookieStore.delete(SESSION_COOKIE);
  cookieStore.delete(REFRESH_COOKIE);
}

export async function getSessionToken(): Promise<string | undefined> {
  const cookieStore = await cookies();
  return cookieStore.get(SESSION_COOKIE)?.value;
}

export async function getRefreshToken(): Promise<string | undefined> {
  const cookieStore = await cookies();
  return cookieStore.get(REFRESH_COOKIE)?.value;
}
