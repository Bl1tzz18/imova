import { getSessionToken } from "@/lib/auth/session";
import type { AdminSummary } from "@/lib/admin/adminSections";

// The admin "things waiting" counts, for the header and the admin tabs. Null when it can't be
// loaded (not signed in, not an admin, API down) — the counts are a hint, never a reason to fail
// a page.
export async function getAdminSummary(): Promise<AdminSummary | null> {
  const token = await getSessionToken();
  if (!token) return null;
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  try {
    const res = await fetch(`${apiUrl}/api/v1/admin/summary`, {
      headers: { Authorization: `Bearer ${token}` },
      cache: "no-store",
    });
    return res.ok ? ((await res.json()) as AdminSummary) : null;
  } catch {
    return null;
  }
}
