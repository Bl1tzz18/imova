import createIntlMiddleware from "next-intl/middleware";
import { NextResponse, type NextRequest } from "next/server";
import { localeCookieName } from "@/i18n/config";
import { alternateLinksHeader, localeRedirect, splitLocale } from "@/i18n/localePaths";
import { routing } from "@/i18n/routing";
import { siteUrl } from "@/lib/site";
import { wwwRedirect } from "@/lib/seo/canonicalHost";
import {
  needsRefresh,
  REFRESH_COOKIE,
  SESSION_COOKIE,
  sessionCookies,
  type SessionTokens,
} from "@/lib/auth/sessionCookies";

const intlMiddleware = createIntlMiddleware(routing);

type SessionCookie = ReturnType<typeof sessionCookies>[number];

// Every request, in this order:
// 0. www.imova.md → imova.md, permanently (lib/seo/canonicalHost).
// 1. Session: keeps a signed-in visitor signed in. The login token only lives 15 minutes, so an
//    expired (or nearly expired) one is renewed from the refresh token before any page, server
//    action or route handler runs. The new tokens go both onto this request — so what renders now
//    already uses them — and onto the response as cookies. Server Components can't set cookies
//    themselves, which is why this happens here and not in getSessionToken().
// 2. Language: an unprefixed page visit by someone whose language is Russian or English goes to
//    that language's address (localeRedirect); then next-intl maps the address onto the [locale]
//    segment (/ru/search → [locale]=ru, /search → [locale]=ro, /ro/search → redirect to /search).
// 3. hreflang: every page answers with its addresses in all three languages (Link header).
export async function middleware(request: NextRequest) {
  const canonical = wwwRedirect(request.headers.get("host"), siteUrl(), request.nextUrl.pathname, request.nextUrl.search);
  if (canonical) return NextResponse.redirect(canonical, 308);

  const session = await renewSession(request);

  const target = localeRedirect({
    method: request.method,
    pathname: request.nextUrl.pathname,
    search: request.nextUrl.search,
    secFetchDest: request.headers.get("sec-fetch-dest"),
    cookieLocale: request.cookies.get(localeCookieName)?.value,
  });
  const response = target ? NextResponse.redirect(new URL(target, siteUrl()), 307) : intlMiddleware(request);

  publicLocation(response, request);
  applySession(response, session);

  if (request.method === "GET" && !response.headers.has("location")) {
    const { path } = splitLocale(request.nextUrl.pathname);
    response.headers.set("Link", alternateLinksHeader(siteUrl(), path, request.nextUrl.search));
  }

  return response;
}

type SessionChange = { kind: "none" } | { kind: "ended" } | { kind: "renewed"; cookies: SessionCookie[] };

async function renewSession(request: NextRequest): Promise<SessionChange> {
  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;
  if (!refreshToken || !needsRefresh(request.cookies.get(SESSION_COOKIE)?.value, Date.now())) {
    return { kind: "none" };
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
    return { kind: "none" };
  }

  if (res.status === 400 || res.status === 401) {
    // The session is over (signed out elsewhere, password changed, expired): drop both cookies so
    // the page renders signed out.
    request.cookies.delete(SESSION_COOKIE);
    request.cookies.delete(REFRESH_COOKIE);
    return { kind: "ended" };
  }

  if (!res.ok) {
    return { kind: "none" };
  }

  // Written onto the request's own cookie header, which next-intl forwards to the render.
  const cookies = sessionCookies((await res.json()) as SessionTokens, process.env.NODE_ENV === "production");
  for (const { name, value } of cookies) {
    request.cookies.set(name, value);
  }
  return { kind: "renewed", cookies };
}

function applySession(response: NextResponse, session: SessionChange) {
  if (session.kind === "ended") {
    response.cookies.delete(SESSION_COOKIE);
    response.cookies.delete(REFRESH_COOKIE);
  } else if (session.kind === "renewed") {
    for (const { name, value, options } of session.cookies) {
      response.cookies.set(name, value, options);
    }
  }
}

// Redirects point at the site's public address (SITE_URL): inside docker the request's own origin
// is the container's (0.0.0.0:3000), never the address in the browser — and middleware can't send a
// relative Location.
function publicLocation(response: NextResponse, request: NextRequest) {
  const location = response.headers.get("location");
  if (!location) return;
  const url = new URL(location, request.url);
  if (url.origin === request.nextUrl.origin) {
    response.headers.set("location", `${siteUrl()}${url.pathname}${url.search}`);
  }
}

export const config = {
  runtime: "nodejs",
  // Everything except Next's own files and static files (images, sitemap*.xml, robots.txt).
  matcher: ["/((?!_next/static|_next/image|favicon.ico|.*\\.(?:png|jpg|jpeg|gif|svg|webp|avif|ico|txt|xml)$).*)"],
};
