"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiErrorMessage } from "@/lib/api/errorMessage";
import { getSessionToken } from "@/lib/auth/session";

// Verify or unverify an agency (/admin/agencies). The API is admin-only at the route; verifying
// emails the agency's Owners.
export async function setAgencyVerified(agencyId: string, verified: boolean): Promise<{ error?: string }> {
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/agencies");
  }

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/admin/agencies/${encodeURIComponent(agencyId)}/${verified ? "verify" : "unverify"}`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  revalidatePath("/admin/agencies");
  return {};
}
