"use server";

import { revalidatePath } from "next/cache";
import { getTranslations } from "next-intl/server";
import { apiErrorMessage } from "@/lib/api/errorMessage";
import { getSessionToken } from "@/lib/auth/session";
import { SAVED_SEARCHES_PATH, type AlertFrequency, type SavedSearch } from "@/lib/savedSearches/savedSearch";

// Saved-search calls from client components (save button, the /saved-searches page), made on the
// server because the API needs the session token (an httpOnly cookie).

type Result<T = object> = ({ error?: undefined } & T) | { error: string };

async function call(path: string, init: RequestInit): Promise<Response | null> {
  const token = await getSessionToken();
  if (!token) return null;
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  return fetch(`${apiUrl}/api/v1/saved-searches${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    cache: "no-store",
  });
}

async function failure(res: Response | null): Promise<{ error: string }> {
  if (!res) return { error: (await getTranslations("Errors"))("status.401") };
  return { error: await apiErrorMessage(res) };
}

export async function saveSearch(
  name: string,
  queryString: string,
  alertFrequency: AlertFrequency,
): Promise<Result<{ savedSearch: SavedSearch }>> {
  const res = await call("", { method: "POST", body: JSON.stringify({ name, queryString, alertFrequency }) });
  if (!res?.ok) return failure(res);
  revalidatePath(SAVED_SEARCHES_PATH);
  return { savedSearch: (await res.json()) as SavedSearch };
}

export async function updateSavedSearch(
  id: string,
  name: string,
  alertFrequency: AlertFrequency,
): Promise<Result<{ savedSearch: SavedSearch }>> {
  const res = await call(`/${id}`, { method: "PUT", body: JSON.stringify({ name, alertFrequency }) });
  if (!res?.ok) return failure(res);
  revalidatePath(SAVED_SEARCHES_PATH);
  return { savedSearch: (await res.json()) as SavedSearch };
}

export async function deleteSavedSearch(id: string): Promise<Result> {
  const res = await call(`/${id}`, { method: "DELETE" });
  if (!res || (!res.ok && res.status !== 404)) return failure(res);
  revalidatePath(SAVED_SEARCHES_PATH);
  return {};
}
