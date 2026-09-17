import { getBrowserApiUrl } from "@/lib/api/media";

// Called from the browser (GoogleSignInButton), on every mount — see that component for why
// this can't just be a value fetched once server-side and passed down as a prop.
export async function fetchGoogleClientId(): Promise<string> {
  try {
    const res = await fetch(`${getBrowserApiUrl()}/api/v1/auth/config`);
    if (!res.ok) return "";
    const data = (await res.json()) as { googleClientId?: string };
    return data.googleClientId ?? "";
  } catch {
    return "";
  }
}
