// Settings the browser needs that differ per environment (dev, prod) — read when the server starts,
// not baked in at build time, so one web image runs in every environment. The root layout renders
// them as a JSON <script> (never executed, so no inline-script concerns); browser code reads them
// back with readPublicConfig.

export const PUBLIC_CONFIG_ELEMENT_ID = "imova-public-config";

export type PublicConfig = { apiUrl: string };

const LOCAL_API_URL = "http://localhost:8080";

// Server side: from the environment. PUBLIC_API_URL is the deployed setting; NEXT_PUBLIC_API_URL
// still works for a local `npm run dev` that sets it.
export function serverPublicConfig(env: Record<string, string | undefined> = process.env): PublicConfig {
  const apiUrl = env.PUBLIC_API_URL || env.NEXT_PUBLIC_API_URL || LOCAL_API_URL;
  return { apiUrl: apiUrl.replace(/\/+$/, "") };
}

// The <script> body. "<" is escaped so a value can never close the script element early.
export function serializePublicConfig(config: PublicConfig): string {
  return JSON.stringify(config).replace(/</g, "\\u003c");
}

// Back from the <script> body; anything missing or malformed falls back to the local default.
export function parsePublicConfig(text: string | null | undefined): PublicConfig {
  try {
    const value: unknown = JSON.parse(text ?? "");
    if (value && typeof value === "object" && typeof (value as PublicConfig).apiUrl === "string" && (value as PublicConfig).apiUrl) {
      return { apiUrl: (value as PublicConfig).apiUrl };
    }
  } catch {
    // fall through
  }
  return { apiUrl: LOCAL_API_URL };
}

let cached: PublicConfig | null = null;

// Browser: the layout's JSON element; on the server (no document): the environment.
export function readPublicConfig(): PublicConfig {
  if (typeof document === "undefined") return serverPublicConfig();
  cached ??= parsePublicConfig(document.getElementById(PUBLIC_CONFIG_ELEMENT_ID)?.textContent);
  return cached;
}
