"use server";

import { revalidatePath } from "next/cache";
import { getTranslations } from "next-intl/server";
import { getSessionToken } from "@/lib/auth/session";

export type CreatePropertyState = {
  error?: string;
  success?: boolean;
};

function optionalNumber(value: FormDataEntryValue | null): number | null {
  if (value === null || value === "") return null;
  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

function optionalBoolean(value: FormDataEntryValue | null): boolean | null {
  if (value === "true") return true;
  if (value === "false") return false;
  return null;
}

export async function createProperty(
  _prevState: CreatePropertyState,
  formData: FormData
): Promise<CreatePropertyState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const payload = {
    id: formData.get("id") || null,
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
    area: optionalNumber(formData.get("area")),
    rooms: optionalNumber(formData.get("rooms")),
    bathrooms: optionalNumber(formData.get("bathrooms")),
    floor: optionalNumber(formData.get("floor")),
    totalFloors: optionalNumber(formData.get("totalFloors")),
    yearBuilt: optionalNumber(formData.get("yearBuilt")),
    furnished: optionalBoolean(formData.get("furnished")),
    parkingAvailable: optionalBoolean(formData.get("parkingAvailable")),
    petsAllowed: optionalBoolean(formData.get("petsAllowed")),
  };

  const token = await getSessionToken();
  const res = await fetch(`${apiUrl}/api/v1/properties`, {
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

  revalidatePath("/");
  return { success: true };
}
