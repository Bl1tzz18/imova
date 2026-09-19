"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { getTranslations } from "next-intl/server";
import { getSessionToken } from "@/lib/auth/session";

async function readActionError(res: Response): Promise<{ error?: string }> {
  const problem = await res.json().catch(() => null);
  const message = problem?.errors
    ? Object.values(problem.errors as Record<string, string[]>).flat().join(" ")
    : ((problem?.detail as string | undefined) ?? (await getTranslations("AdminModerationPage"))("actionError"));
  return { error: message };
}

export async function approveListing(propertyId: string): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/moderation");
  }

  const res = await fetch(`${apiUrl}/api/v1/properties/${propertyId}/approve`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    return readActionError(res);
  }

  revalidatePath("/admin/moderation");
  return {};
}

export async function rejectListing(propertyId: string, reason: string): Promise<{ error?: string }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/moderation");
  }

  const res = await fetch(`${apiUrl}/api/v1/properties/${propertyId}/reject`, {
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
