"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { getTranslations } from "next-intl/server";

export type CreatePropertyState = {
  error?: string;
};

export async function createProperty(
  _prevState: CreatePropertyState,
  formData: FormData
): Promise<CreatePropertyState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const payload = {
    ownerId: formData.get("ownerId"),
    title: formData.get("title"),
    description: formData.get("description"),
    propertyType: formData.get("propertyType"),
    listingType: formData.get("listingType"),
    price: Number(formData.get("price")),
    currency: formData.get("currency"),
    country: formData.get("country"),
    city: formData.get("city"),
    district: formData.get("district") || null,
    latitude: Number(formData.get("latitude")),
    longitude: Number(formData.get("longitude")),
  };

  const res = await fetch(`${apiUrl}/api/v1/properties`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const problem = await res.json().catch(() => null);
    const message = problem?.errors
      ? Object.values(problem.errors as Record<string, string[]>).flat().join(" ")
      : await getTranslations("PropertyForm").then((t) => t("genericError", { status: res.status }));

    return { error: message };
  }

  revalidatePath("/");
  redirect("/");
}
