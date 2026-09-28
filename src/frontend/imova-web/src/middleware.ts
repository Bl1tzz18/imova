import { NextResponse, type NextRequest } from "next/server";
import {
  needsRefresh,
  REFRESH_COOKIE,
  SESSION_COOKIE,
  sessionCookies,
  type SessionTokens,
} from "@/lib/auth/sessionCookies";

// Keeps a signed-in visitor signed in: the login token only lives 15 minutes, so before any page,
// server action or route handler runs, an expired (or nearly expired) one is renewed from the
// refresh token. The new tokens go both onto this request — so what renders now already uses
// them — and onto the response as cookies. Server Components can't set cookies themselves, which
// is why this happens here and not in getSessionToken().
export async function middleware(request: NextRequest) {
  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;
  if (!refreshToken || !needsRefresh(request.cookies.get(SESSION_COOKIE)?.value, Date.now())) {
    return NextResponse.next();
  }

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  let res: Response;
  try {
    res = await fetch(`${apiUrl}/api/v1/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
      cache: "no-store",
    });
  } catch {
    // API unreachable: carry on as-is; the next request tries again.
    return NextResponse.next();
  }

  if (res.status === 400 || res.status === 401) {
    // The session is over (signed out elsewhere, password changed, expired): drop both cookies so
    // the page renders signed out.
    request.cookies.delete(SESSION_COOKIE);
    request.cookies.delete(REFRESH_COOKIE);
    const response = NextResponse.next({ request: { headers: request.headers } });
    response.cookies.delete(SESSION_COOKIE);
    response.cookies.delete(REFRESH_COOKIE);
    return response;
  }

  if (!res.ok) {
    return NextResponse.next();
  }

  const cookies = sessionCookies((await res.json()) as SessionTokens, process.env.NODE_ENV === "production");
  for (const { name, value } of cookies) {
    request.cookies.set(name, value);
  }

  const response = NextResponse.next({ request: { headers: request.headers } });
  for (const { name, value, options } of cookies) {
    response.cookies.set(name, value, options);
  }

  return response;
}

export const config = {
  runtime: "nodejs",
  // Everything except static files and images.
  matcher: ["/((?!_next/static|_next/image|favicon.ico|.*\\.(?:png|jpg|jpeg|gif|svg|webp|ico|txt|xml)$).*)"],
};
