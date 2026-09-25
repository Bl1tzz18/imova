import { getSessionToken } from "@/lib/auth/session";

// Same-origin proxy for message images: the API only serves one to the conversation's two
// participants (or an admin), and needs the caller's identity to decide — which an <img> tag
// pointing at the API couldn't send (the session token is an httpOnly cookie here). So the image
// is fetched server-side with that token and streamed back; the API's 401/404 pass through.
export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const token = await getSessionToken();
  if (!token) {
    return new Response(null, { status: 401 });
  }

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/messaging/attachments/${encodeURIComponent(id)}`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });
  if (!res.ok || !res.body) {
    return new Response(null, { status: res.status === 401 ? 401 : 404 });
  }

  return new Response(res.body, {
    headers: {
      "Content-Type": res.headers.get("Content-Type") ?? "application/octet-stream",
      "Cache-Control": "private, max-age=3600",
    },
  });
}
