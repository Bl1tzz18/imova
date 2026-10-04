import { getSessionToken } from "@/lib/auth/session";

// Which optional emails the user gets (GET/PUT /api/v1/users/me/email-preferences).
// favoriteUpdates: a saved listing changed price or is no longer available.
export type EmailPreferences = { favoriteUpdates: boolean };

const apiUrl = () => process.env.API_URL ?? "http://localhost:8080";

// For the Server Component render of /account — null when signed out or the API can't answer
// (the Notifications tab then says it couldn't load them).
export async function getEmailPreferences(): Promise<EmailPreferences | null> {
  const token = await getSessionToken();
  if (!token) {
    return null;
  }

  const res = await fetch(`${apiUrl()}/api/v1/users/me/email-preferences`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });
  return res.ok ? ((await res.json()) as EmailPreferences) : null;
}

// The one-click "stop these emails" link in a saved-listing email — no sign-in.
export async function unsubscribeFavoriteAlerts(userId: string, token: string): Promise<"done" | "gone" | "invalid"> {
  const res = await fetch(`${apiUrl()}/api/v1/favorites/alerts/unsubscribe`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId, token }),
    cache: "no-store",
  });
  if (res.status === 404) return "gone";
  return res.ok ? "done" : "invalid";
}
