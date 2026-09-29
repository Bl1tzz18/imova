import { getSessionToken } from "@/lib/auth/session";
import type { AccountDataSummary } from "@/lib/account/deletion";

// For the Server Component render of /account (like getCurrentUserProfile) — null when signed out
// or when the API can't answer; the privacy tab then just leaves the counts out.
export async function getAccountDataSummary(): Promise<AccountDataSummary | null> {
  const token = await getSessionToken();
  if (!token) {
    return null;
  }

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/users/me/data-summary`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  return res.ok ? ((await res.json()) as AccountDataSummary) : null;
}
