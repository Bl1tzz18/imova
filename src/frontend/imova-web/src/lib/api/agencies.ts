import type { MyAgency } from "@/types/agency";

// Server-side only (needs the session token): the agencies the signed-in user belongs to, with
// their role — for the listing form's "Publică ca" and who may manage which listing.
export async function getMyAgencies(token: string): Promise<MyAgency[]> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/users/me/agencies`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch agencies: ${res.status}`);
  }

  return res.json();
}
