import { headers } from "next/headers";

// The API rate-limits its auth endpoints per client IP (AuthRateLimiting on the backend). Server
// actions call it from this server, so without this every visitor would look like one client.
//
// The rightmost X-Forwarded-For entry is the one added by the proxy closest to us (a reverse proxy
// in front of Next appends the address it saw). Next itself only fills the header in when it's
// absent, so with no reverse proxy in front a visitor could send their own — production must run
// behind one.
export function clientIpFrom(forwardedFor: string | null, realIp: string | null): string | null {
  const last = forwardedFor?.split(",").map((part) => part.trim()).filter(Boolean).at(-1);
  return last || realIp?.trim() || null;
}

export async function forwardedForHeader(): Promise<Record<string, string>> {
  const incoming = await headers();
  const ip = clientIpFrom(incoming.get("x-forwarded-for"), incoming.get("x-real-ip"));
  return ip ? { "X-Forwarded-For": ip } : {};
}
