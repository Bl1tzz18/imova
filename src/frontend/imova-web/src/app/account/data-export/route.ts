import { getSessionToken } from "@/lib/auth/session";

// "Download my data": streams the API's ZIP (every piece of data and every file of the account)
// through the web app, since the session token is an httpOnly cookie the browser can't send to the
// API itself. Linked with a plain <a> (never next/link — a prefetch would build an export). On
// failure the user lands back on the privacy tab with the reason, instead of a broken download.
export async function GET() {
  const token = await getSessionToken();
  if (!token) {
    return redirectTo("/login?next=/account?tab=privacy");
  }

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/users/me/data-export`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (res.status === 401) {
    return redirectTo("/login?next=/account?tab=privacy");
  }

  if (!res.ok || !res.body) {
    return redirectTo(`/account?tab=privacy&export=${res.status === 429 ? "tooSoon" : "failed"}`);
  }

  return new Response(res.body, {
    headers: {
      "Content-Type": res.headers.get("Content-Type") ?? "application/zip",
      "Content-Disposition": res.headers.get("Content-Disposition") ?? 'attachment; filename="imova-date-personale.zip"',
      "Cache-Control": "no-store",
    },
  });
}

// Relative: inside docker the request's own origin is the container's (0.0.0.0:3000).
function redirectTo(location: string) {
  return new Response(null, { status: 303, headers: { Location: location } });
}
