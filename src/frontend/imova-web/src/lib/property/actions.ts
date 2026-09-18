"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { getSessionToken } from "@/lib/auth/session";

export async function setFavorite(propertyId: string, saved: boolean, next: string): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect(`/login?next=${encodeURIComponent(next)}`);
  }

  const res = await fetch(`${apiUrl}/api/v1/properties/${propertyId}/favorite`, {
    method: saved ? "POST" : "DELETE",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    return { error: `Request failed (${res.status})` };
  }

  revalidatePath("/saved-listings");
  return {};
}
