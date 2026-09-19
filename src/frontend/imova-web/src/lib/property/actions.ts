"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { getTranslations } from "next-intl/server";
import { getSessionToken } from "@/lib/auth/session";
import { buildPropertyPayload } from "@/lib/property/formPayload";

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

async function postPropertyStatusAction(
  propertyId: string,
  action: "submit-for-review" | "archive" | "republish",
): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/my-listings");
  }

  const res = await fetch(`${apiUrl}/api/v1/properties/${propertyId}/${action}`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    const problem = await res.json().catch(() => null);
    const message = problem?.errors
      ? Object.values(problem.errors as Record<string, string[]>).flat().join(" ")
      : ((problem?.detail as string | undefined) ?? (await getTranslations("MyListingsPage"))("actionError"));
    return { error: message };
  }

  revalidatePath("/my-listings");
  return {};
}

// Submits a Draft (or resubmits a Rejected) listing for admin review — see
// Property.SubmitForReview(). Never goes straight to Published; only Approve() (admin-only,
// backend-only for now) does that.
export async function submitForReview(propertyId: string): Promise<{ error?: string }> {
  return postPropertyStatusAction(propertyId, "submit-for-review");
}

export async function archiveProperty(propertyId: string): Promise<{ error?: string }> {
  return postPropertyStatusAction(propertyId, "archive");
}

export async function republishProperty(propertyId: string): Promise<{ error?: string }> {
  return postPropertyStatusAction(propertyId, "republish");
}

export type UpdatePropertyState = { error?: string; success?: boolean };

// Bound with the property id from the client (updatePropertyDetails.bind(null, id)) so it fits
// useActionState's (prevState, formData) signature — see PropertyForm, which reuses the exact
// same multi-step create flow (and therefore the same field set) for editing.
export async function updatePropertyDetails(
  propertyId: string,
  _prevState: UpdatePropertyState,
  formData: FormData,
): Promise<UpdatePropertyState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect(`/login?next=${encodeURIComponent(`/my-listings/${propertyId}/edit`)}`);
  }

  const res = await fetch(`${apiUrl}/api/v1/properties/${propertyId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify(buildPropertyPayload(formData)),
  });

  if (!res.ok) {
    const problem = await res.json().catch(() => null);
    const message = problem?.errors
      ? Object.values(problem.errors as Record<string, string[]>).flat().join(" ")
      : await getTranslations("PropertyForm").then((t) => t("genericError", { status: res.status }));
    return { error: message };
  }

  revalidatePath("/my-listings");
  revalidatePath(`/my-listings/${propertyId}/edit`);
  revalidatePath(`/property/${propertyId}`);
  return { success: true };
}

// Called directly from the client (ImageUploader), not via a form action — same pattern as
// setFavorite. Routed through a server action (rather than a direct browser fetch like
// requestUploadUrl/confirmMediaUpload) specifically so the httpOnly session cookie can be
// attached as a Bearer token: the new DELETE endpoint requires auth + an owner/admin check.
export async function deletePropertyMedia(propertyId: string, mediaId: string): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    return { error: "Not authenticated." };
  }

  const res = await fetch(`${apiUrl}/api/v1/properties/${propertyId}/media/${mediaId}`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok && res.status !== 404) {
    return { error: `Request failed (${res.status})` };
  }

  revalidatePath(`/my-listings/${propertyId}/edit`);
  return {};
}
