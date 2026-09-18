import { getSessionToken } from "@/lib/auth/session";

export type UserProfile = {
  id: string;
  email: string;
  displayName: string | null;
  phoneNumber: string | null;
  roles: string[];
  hasPassword: boolean;
};

// Not a Server Action (no "use server" here) — this reads data for a Server Component render,
// not a form mutation, so it doesn't need the server-action wire format.
export async function getCurrentUserProfile(): Promise<UserProfile | null> {
  const token = await getSessionToken();
  if (!token) {
    return null;
  }

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/auth/me`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (!res.ok) {
    return null;
  }

  return (await res.json()) as UserProfile;
}
