import { getSessionToken } from "@/lib/auth/session";

// Server-side only. Passes a file the API serves only to signed-in callers (a CSV template, an import
// report) on to the browser as a download, with the session token — a plain link can then point at
// the web app's own address. The API's status passes through (401/403/404 as text).
export async function proxyFileDownload(apiPath: string): Promise<Response> {
  const token = await getSessionToken();
  if (!token) {
    return new Response("Sign in first.", { status: 401 });
  }

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}${apiPath}`, { headers: { Authorization: `Bearer ${token}` }, cache: "no-store" });
  if (!res.ok) {
    return new Response(res.status === 403 ? "Not allowed." : "Not found.", { status: res.status === 403 ? 403 : 404 });
  }

  const headers = new Headers({ "Cache-Control": "no-store" });
  for (const name of ["content-type", "content-disposition"]) {
    const value = res.headers.get(name);
    if (value) headers.set(name, value);
  }

  return new Response(res.body, { status: 200, headers });
}
