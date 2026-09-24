import type { Publisher } from "@/types/listing";

// Server-side only (needs the session token) — the identities the signed-in user can publish a
// listing under: always their Individual publisher, plus their Agency one if they created it.
export async function getMyPublishers(token: string): Promise<Publisher[]> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/publishers/mine`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch publishers: ${res.status}`);
  }

  return res.json();
}
