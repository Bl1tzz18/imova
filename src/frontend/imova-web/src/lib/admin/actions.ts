"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { getSessionToken } from "@/lib/auth/session";
import { apiErrorMessage } from "@/lib/api/errorMessage";
import { forwardedForHeader } from "@/lib/auth/clientIp";

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

// Closes the open reports on a listing without acting on it; the note is for the other admins only.
// (Acting on it = suspendListing, which closes its reports too.)
export async function dismissListingReports(listingId: string, note: string): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/moderation?tab=reports");
  }

  const res = await fetch(`${apiUrl}/api/v1/admin/listing-reports/${listingId}/dismiss`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify({ note: note.trim() || null }),
  });

  if (!res.ok) {
    return readActionError(res);
  }

  revalidatePath("/admin/moderation");
  return {};
}

export type GrantAdminState = { error?: string; grantedEmail?: string };

// Makes an existing, email-confirmed account an admin. The API checks everything that matters —
// the caller is an admin right now, their own password (with lockout), the target — and records
// and announces it; this only relays the form.
export async function grantAdmin(_prevState: GrantAdminState, formData: FormData): Promise<GrantAdminState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/admins");
  }

  const email = String(formData.get("email") ?? "").trim();
  const res = await fetch(`${apiUrl}/api/v1/admin/admins`, {
    method: "POST",
    // The visitor's IP, for the audit entry (the API trusts it only from this server — see AuthRateLimiting).
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}`, ...(await forwardedForHeader()) },
    body: JSON.stringify({ email, password: formData.get("password") || null }),
  });

  if (!res.ok) {
    return readActionError(res);
  }

  revalidatePath("/admin/admins");
  return { grantedEmail: email };
}
