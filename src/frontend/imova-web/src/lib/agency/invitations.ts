import type { Invitation } from "@/types/agency";

// Server-side only. The invitation behind an emailed link; null when the link matches none
// (mistyped, or replaced by a newer one when the invitation was resent). Public: holding the link
// is what counts.
export async function getInvitation(token: string): Promise<Invitation | null> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/invitations/${encodeURIComponent(token)}`, { cache: "no-store" });

  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Failed to fetch the invitation: ${res.status}`);
  return res.json();
}
