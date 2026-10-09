"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiErrorMessage } from "@/lib/api/errorMessage";
import { getSessionToken } from "@/lib/auth/session";
import { agencyManagePath, MY_AGENCIES_HREF, NEW_AGENCY_HREF } from "@/lib/agency/manage";
import type { AgencyRole } from "@/types/agency";

// The agency management pages' writes. Each returns { error } (translated) instead of throwing; the
// pages refresh themselves afterwards (router.refresh) where the result shows elsewhere on the page.
// The logo goes through a route handler instead (app/account/agencies/[id]/logo) — files are bigger
// than a server action's body limit.

export type ActionResult = { error?: string };

function apiUrl(): string {
  return process.env.API_URL ?? "http://localhost:8080";
}

async function send(path: string, init: RequestInit & { json?: unknown }, loginNext: string): Promise<Response> {
  const token = await getSessionToken();
  if (!token) {
    redirect(`/login?next=${encodeURIComponent(loginNext)}`);
  }
  const { json, ...rest } = init;
  return fetch(`${apiUrl()}${path}`, {
    ...rest,
    headers: {
      Authorization: `Bearer ${token}`,
      ...(json !== undefined ? { "Content-Type": "application/json" } : {}),
    },
    body: json !== undefined ? JSON.stringify(json) : undefined,
  });
}

function text(formData: FormData, name: string): string | null {
  const value = String(formData.get(name) ?? "").trim();
  return value === "" ? null : value;
}

function profileBody(formData: FormData) {
  return {
    name: String(formData.get("name") ?? "").trim(),
    phone: String(formData.get("phone") ?? "").trim(),
    email: text(formData, "email"),
    bio: text(formData, "bio"),
    website: text(formData, "website"),
    address: text(formData, "address"),
    raionId: text(formData, "raionId"),
  };
}

export type AgencyProfileState = { error?: string; savedAt?: number };

// Create: on success, straight to the new agency's page, which asks for a logo next.
export async function createAgency(_prev: AgencyProfileState, formData: FormData): Promise<AgencyProfileState> {
  const res = await send("/api/v1/agencies", { method: "POST", json: profileBody(formData) }, NEW_AGENCY_HREF);
  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }
  const agency = (await res.json()) as { id: string };
  revalidatePath("/account");
  redirect(`${agencyManagePath(agency.id)}?created=1`);
}

export async function updateAgency(_prev: AgencyProfileState, formData: FormData): Promise<AgencyProfileState> {
  const agencyId = String(formData.get("agencyId") ?? "");
  const res = await send(
    `/api/v1/agencies/${encodeURIComponent(agencyId)}`,
    { method: "PUT", json: profileBody(formData) },
    agencyManagePath(agencyId),
  );
  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }
  revalidatePath(agencyManagePath(agencyId), "layout");
  return { savedAt: Date.now() };
}

// --- Deactivate / reactivate / delete (Setări) ---

export async function setAgencyActive(agencyId: string, active: boolean): Promise<ActionResult> {
  const res = await send(
    `/api/v1/agencies/${encodeURIComponent(agencyId)}/${active ? "reactivate" : "deactivate"}`,
    { method: "POST" },
    agencyManagePath(agencyId, "settings"),
  );
  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }
  revalidatePath(agencyManagePath(agencyId), "layout");
  revalidatePath("/account");
  return {};
}

// Deletes for good (the exact name confirms it), then back to "Agențiile mele".
export async function deleteAgency(agencyId: string, confirmName: string): Promise<ActionResult> {
  const res = await send(
    `/api/v1/agencies/${encodeURIComponent(agencyId)}`,
    { method: "DELETE", json: { confirmName } },
    agencyManagePath(agencyId, "settings"),
  );
  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }
  revalidatePath("/account");
  redirect(MY_AGENCIES_HREF);
}

// --- Members ---

export async function changeMemberRole(agencyId: string, userId: string, role: AgencyRole): Promise<ActionResult> {
  const res = await send(
    `/api/v1/agencies/${encodeURIComponent(agencyId)}/members/${encodeURIComponent(userId)}`,
    { method: "PATCH", json: { role } },
    agencyManagePath(agencyId, "members"),
  );
  return res.ok ? {} : { error: await apiErrorMessage(res) };
}

// Removes a member — or, with the caller's own id, leaves. reassignTo: who takes over their listings.
export async function removeMember(agencyId: string, userId: string, reassignTo: string | null): Promise<ActionResult> {
  const query = reassignTo ? `?reassignTo=${encodeURIComponent(reassignTo)}` : "";
  const res = await send(
    `/api/v1/agencies/${encodeURIComponent(agencyId)}/members/${encodeURIComponent(userId)}${query}`,
    { method: "DELETE" },
    agencyManagePath(agencyId, "members"),
  );
  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }
  revalidatePath("/account");
  return {};
}

// --- The agency's invitations ---

export type InviteState = { error?: string; sentTo?: string; sentAt?: number };

export async function inviteMember(_prev: InviteState, formData: FormData): Promise<InviteState> {
  const agencyId = String(formData.get("agencyId") ?? "");
  const email = String(formData.get("email") ?? "").trim();
  const role = formData.get("role") === "Admin" ? "Admin" : "Agent";
  const res = await send(
    `/api/v1/agencies/${encodeURIComponent(agencyId)}/invitations`,
    { method: "POST", json: { email, role } },
    agencyManagePath(agencyId, "members"),
  );
  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }
  return { sentTo: email, sentAt: Date.now() };
}

export async function resendInvitation(agencyId: string, invitationId: string): Promise<ActionResult> {
  const res = await send(
    `/api/v1/agencies/${encodeURIComponent(agencyId)}/invitations/${encodeURIComponent(invitationId)}/resend`,
    { method: "POST" },
    agencyManagePath(agencyId, "members"),
  );
  return res.ok ? {} : { error: await apiErrorMessage(res) };
}

export async function revokeInvitation(agencyId: string, invitationId: string): Promise<ActionResult> {
  const res = await send(
    `/api/v1/agencies/${encodeURIComponent(agencyId)}/invitations/${encodeURIComponent(invitationId)}`,
    { method: "DELETE" },
    agencyManagePath(agencyId, "members"),
  );
  return res.ok ? {} : { error: await apiErrorMessage(res) };
}

// --- The signed-in account's own invitations (/account "Agențiile mele") ---

export async function respondToMyInvitation(invitationId: string, answer: "accept" | "decline"): Promise<ActionResult> {
  const res = await send(
    `/api/v1/users/me/invitations/${encodeURIComponent(invitationId)}/${answer}`,
    { method: "POST" },
    MY_AGENCIES_HREF,
  );
  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }
  revalidatePath("/account");
  return {};
}
