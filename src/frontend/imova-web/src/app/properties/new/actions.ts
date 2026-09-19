"use server";

import { revalidatePath } from "next/cache";
import { getTranslations } from "next-intl/server";
import { getSessionToken } from "@/lib/auth/session";
import { buildPropertyPayload } from "@/lib/property/formPayload";

export type CreatePropertyState = {
  error?: string;
  success?: boolean;
};

export async function createProperty(
  _prevState: CreatePropertyState,
  formData: FormData
): Promise<CreatePropertyState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const payload = {
    id: formData.get("id") || null,
    ...buildPropertyPayload(formData),
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
