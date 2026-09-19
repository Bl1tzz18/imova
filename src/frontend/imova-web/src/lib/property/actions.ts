"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { getTranslations } from "next-intl/server";
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
// useActionState's (prevState, formData) signature — see EditListingForm.
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
    body: JSON.stringify({
      title: formData.get("title"),
      description: formData.get("description"),
      price: Number(formData.get("price")),
    }),
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
