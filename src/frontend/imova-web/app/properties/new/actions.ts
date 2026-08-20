"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";

export type CreatePropertyState = {
  error?: string;
};

export async function createProperty(
  _prevState: CreatePropertyState,
  formData: FormData
): Promise<CreatePropertyState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";

  const payload = {
    title: formData.get("title"),
    price: Number(formData.get("price")),
    currency: formData.get("currency"),
    city: formData.get("city"),
    district: formData.get("district"),
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
      : `Eroare la salvare (${res.status}).`;

    return { error: message };
  }

  revalidatePath("/");
  redirect("/");
}
