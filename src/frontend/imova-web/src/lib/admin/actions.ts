"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { getSessionToken } from "@/lib/auth/session";
import { apiErrorMessage } from "@/lib/api/errorMessage";

async function readActionError(res: Response): Promise<{ error?: string }> {
  return { error: await apiErrorMessage(res) };
}

export async function approveListing(listingId: string): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/moderation");
  }

  const res = await fetch(`${apiUrl}/api/v1/listings/${listingId}/approve`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    return readActionError(res);
  }

  revalidatePath("/admin/moderation");
  return {};
}

export async function rejectListing(listingId: string, reason: string): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/moderation");
  }

  const res = await fetch(`${apiUrl}/api/v1/listings/${listingId}/reject`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify({ reason }),
  });

  if (!res.ok) {
    return readActionError(res);
  }

  revalidatePath("/admin/moderation");
  return {};
}

// Takes an Active listing down (the owner sees the reason); only an admin can undo it.
export async function suspendListing(listingId: string, reason: string): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/moderation");
  }

  const res = await fetch(`${apiUrl}/api/v1/listings/${listingId}/suspend`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify({ reason }),
  });

  if (!res.ok) {
    return readActionError(res);
  }

  revalidatePath("/admin/moderation");
  return {};
}

// Suspended -> Active again (a fresh 6-month period if the old one ran out meanwhile).
export async function reinstateListing(listingId: string): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/moderation");
  }

  const res = await fetch(`${apiUrl}/api/v1/listings/${listingId}/reinstate`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    return readActionError(res);
  }

  revalidatePath("/admin/moderation");
  return {};
}
