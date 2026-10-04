"use server";

import { redirect } from "next/navigation";
import { apiErrorMessage } from "@/lib/api/errorMessage";
import { getSessionToken } from "@/lib/auth/session";
import { invitationPath } from "@/lib/agency/invitationPage";

export type InvitationResponseState = { outcome?: "accepted" | "declined"; error?: string };

// Accept (needs the invited account signed in) or decline (the link alone is enough) an invitation.
export async function respondToInvitation(
  _prevState: InvitationResponseState,
  formData: FormData,
): Promise<InvitationResponseState> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = String(formData.get("token") ?? "");
  const answer = formData.get("answer") === "decline" ? "decline" : "accept";
  const session = await getSessionToken();

  if (answer === "accept" && !session) {
    redirect(`/login?next=${encodeURIComponent(invitationPath(token))}`);
  }

  const res = await fetch(`${apiUrl}/api/v1/invitations/${encodeURIComponent(token)}/${answer}`, {
    method: "POST",
    headers: session ? { Authorization: `Bearer ${session}` } : {},
  });

  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  return { outcome: answer === "accept" ? "accepted" : "declined" };
}
