"use server";

import { revalidatePath } from "next/cache";
import { redirectTo } from "@/i18n/redirect";
import { apiErrorMessage } from "@/lib/api/errorMessage";
import { getSessionToken } from "@/lib/auth/session";

// Verify or unverify an agency (/admin/agencies). The API is admin-only at the route; verifying
// emails the agency's Owners.
export async function setAgencyVerified(agencyId: string, verified: boolean): Promise<{ error?: string }> {
  const token = await getSessionToken();
  if (!token) {
    return redirectTo("/login?next=/admin/agencies");
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

// Delete an agency for good (/admin/agencies): its exact name and a reason, which is emailed to the
// members. Its listings stay, as their authors' private listings.
export async function adminDeleteAgency(agencyId: string, confirmName: string, reason: string): Promise<{ error?: string }> {
  const token = await getSessionToken();
  if (!token) {
    return redirectTo("/login?next=/admin/agencies");
  }

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/admin/agencies/${encodeURIComponent(agencyId)}`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json" },
    body: JSON.stringify({ confirmName, reason }),
  });
  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  revalidatePath("/admin/agencies");
  return {};
}
