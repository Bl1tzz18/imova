"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { getTranslations } from "next-intl/server";
import { getSessionToken } from "@/lib/auth/session";
import { buildListingPayload } from "@/lib/property/formPayload";
import type { Photo } from "@/types/listing";
import { apiErrorMessage } from "@/lib/api/errorMessage";

export async function setFavorite(listingId: string, saved: boolean, next: string): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect(`/login?next=${encodeURIComponent(next)}`);
  }

  const res = await fetch(`${apiUrl}/api/v1/listings/${listingId}/favorite`, {
    method: saved ? "POST" : "DELETE",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  revalidatePath("/saved-listings");
  return {};
}

async function postListingStatusAction(
  listingId: string,
  action: "submit-for-review" | "archive" | "publish" | "renew" | "mark-as-sold" | "mark-as-rented",
): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/my-listings");
  }

  const res = await fetch(`${apiUrl}/api/v1/listings/${listingId}/${action}`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  revalidatePath("/my-listings");
  revalidatePath(`/property/${listingId}`);
  return {};
}

// Submits a Draft (or resubmits a Rejected) listing for admin review — see
// Listing.SubmitForReview(). Never goes straight to Active; only Approve() (admin-only) does that.
export async function submitForReview(listingId: string): Promise<{ error?: string }> {
  return postListingStatusAction(listingId, "submit-for-review");
}

export async function archiveListing(listingId: string): Promise<{ error?: string }> {
  return postListingStatusAction(listingId, "archive");
}

// The owner re-activating their own Archived/Expired listing — see Listing.Publish().
export async function publishListing(listingId: string): Promise<{ error?: string }> {
  return postListingStatusAction(listingId, "publish");
}

// The owner keeping their Active listing live for another 6 months — see Listing.Renew().
export async function renewListing(listingId: string): Promise<{ error?: string }> {
  return postListingStatusAction(listingId, "renew");
}

// The deal is done: an Active sale → Sold, an Active rental → Rented (Listing.MarkAsSold/
// MarkAsRented). Visitors then see that it's no longer available; the owner can still deactivate it.
export async function markListingAsSold(listingId: string): Promise<{ error?: string }> {
  return postListingStatusAction(listingId, "mark-as-sold");
}

export async function markListingAsRented(listingId: string): Promise<{ error?: string }> {
  return postListingStatusAction(listingId, "mark-as-rented");
}

export type UpdateListingState = { error?: string; success?: boolean };

// Bound with the listing id from the client (updateListingDetails.bind(null, id)) so it fits
// useActionState's (prevState, formData) signature — see PropertyForm, which reuses the exact
// same multi-step create flow (and therefore the same field set) for editing.
export async function updateListingDetails(
  listingId: string,
  _prevState: UpdateListingState,
  formData: FormData,
): Promise<UpdateListingState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect(`/login?next=${encodeURIComponent(`/my-listings/${listingId}/edit`)}`);
  }

  const res = await fetch(`${apiUrl}/api/v1/listings/${listingId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify(buildListingPayload(formData)),
  });

  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  // Photos the owner removed on this edit page (ImageUploader, deferDeletes mode) are only
  // hidden client-side up to this point — this is the actual commit, and it only runs once the
  // listing update above has already succeeded, so an abandoned/failed edit never deletes them.
  const deleteMediaIds = formData.getAll("deleteMediaIds").filter((id): id is string => typeof id === "string");
  await Promise.allSettled(
    deleteMediaIds.map((mediaId) =>
      fetch(`${apiUrl}/api/v1/listings/${listingId}/media/${mediaId}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${token}` },
      }),
    ),
  );

  revalidatePath("/my-listings");
  revalidatePath(`/my-listings/${listingId}/edit`);
  revalidatePath(`/property/${listingId}`);
  return { success: true };
}

// Called directly from the client (ImageUploader), not via a form action — same pattern as
// setFavorite. Routed through a server action (rather than a direct browser fetch like
// requestUploadUrl/confirmMediaUpload) specifically so the httpOnly session cookie can be
// attached as a Bearer token: the new DELETE endpoint requires auth + an owner/admin check.
export async function deleteListingPhoto(listingId: string, mediaId: string): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    return { error: (await getTranslations("Errors"))("status.401") };
  }

  const res = await fetch(`${apiUrl}/api/v1/listings/${listingId}/media/${mediaId}`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok && res.status !== 404) {
    return { error: await apiErrorMessage(res) };
  }

  revalidatePath(`/my-listings/${listingId}/edit`);
  return {};
}

// The two authenticated halves of a photo upload (the file itself goes straight from the browser
// to storage via the SAS URL — see uploadFileToBlob). Server actions because the API needs the
// session token, which lives in an httpOnly cookie the browser can't read. They return errors
// instead of throwing: a thrown server-action error reaches the client with its message masked.
export type PhotoActionResult<T> = { data: T; error?: undefined } | { data?: undefined; error: string };

async function postPhotoCall<T>(path: string, body: unknown): Promise<PhotoActionResult<T>> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    return { error: (await getTranslations("Errors"))("status.401") };
  }

  const res = await fetch(`${apiUrl}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify(body),
  });

  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  return { data: (await res.json()) as T };
}

export async function requestPhotoUploadUrl(
  listingId: string,
  fileExtension: string,
): Promise<PhotoActionResult<{ uploadUrl: string; blobName: string; expiresAt: string }>> {
  return postPhotoCall(`/api/v1/listings/${listingId}/media/upload-url`, { fileExtension });
}

export async function confirmPhotoUpload(listingId: string, blobName: string): Promise<PhotoActionResult<Photo>> {
  return postPhotoCall(`/api/v1/listings/${listingId}/media/confirm`, { blobName });
}
