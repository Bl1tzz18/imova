"use server";

import { revalidatePath } from "next/cache";
import { getTranslations } from "next-intl/server";
import { getSessionToken } from "@/lib/auth/session";
import { buildListingPayload } from "@/lib/property/formPayload";

export type CreateListingState = {
  error?: string;
  success?: boolean;
  // The new listing stayed a Draft because the owner's email isn't confirmed yet — it goes to
  // review by itself once they open the confirmation link.
  awaitingEmailConfirmation?: boolean;
};

// Creates the Property and its Listing together in one request (the backend saves both
// atomically — see CreateListingHandler).
export async function createListing(
  _prevState: CreateListingState,
  formData: FormData
): Promise<CreateListingState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const payload = {
    id: formData.get("id") || null,
    // Omitted = the user's own Individual publisher; only sent when they picked their agency.
    publisherId: formData.get("publisherId") || null,
    ...buildListingPayload(formData),
  };

  const token = await getSessionToken();
  const res = await fetch(`${apiUrl}/api/v1/listings`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const problem = await res.json().catch(() => null);
    const message = problem?.errors
      ? Object.values(problem.errors as Record<string, string[]>).flat().join(" ")
      : await getTranslations("PropertyForm").then((t) => t("genericError", { status: res.status }));

    return { error: message };
  }

  const created = (await res.json()) as { status: string };
  revalidatePath("/");
  return { success: true, awaitingEmailConfirmation: created.status === "Draft" };
}
