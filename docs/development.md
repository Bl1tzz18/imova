# Development guide

How to run, build, and test the IMOVA stack locally — Docker Compose commands, the
backend-only/frontend-only dev loops, EF Core migrations, and a `dotnet test`/`vstest` quirk
specific to some sandboxes.

## Running the stack

Run all services together via Docker Compose — this is the primary way to run and test the app as
a whole (it's the only way to get PostGIS, and matches how it actually deploys):

```bash
docker compose up -d --build   # build and start postgres, backend, frontend
docker compose ps              # check status
docker compose logs backend    # or frontend / postgres
docker compose down            # stop (add -v to also drop the postgres volume)
```

The backend applies EF Core migrations automatically on startup (`dbContext.Database.Migrate()` in
`Program.cs`) — no manual migration step needed to run the stack.

A local .NET 10 SDK lets `dotnet build`/`dotnet run`/`dotnet ef` work directly without a
container — much faster for quick build/test loops than round-tripping through Docker. Use it for
that; still use Docker Compose to actually run/verify the app, since PostGIS only exists there. To
add a migration after changing an entity or its configuration, from the repo root:

```bash
dotnet tool install --tool-path /tmp/ef-tools dotnet-ef   # once per shell/session
/tmp/ef-tools/dotnet-ef migrations add <Name> \
  --project src/backend/Imova.Infrastructure --startup-project src/backend/Imova.Api
```

Build/test the whole solution the normal way — `dotnet build src/backend/Imova.sln`. For running
tests, `dotnet test`'s own console output has been observed to go silent in some sandboxes (exit
code 0, no output) while `dotnet vstest <path-to-dll>` printed normally; try `dotnet test` first
and fall back to `dotnet vstest tests/<Project>/bin/Debug/net10.0/<Project>.dll` if it goes quiet.

Integration tests use their **own database**, `imova_test`, on the same compose Postgres — never the
dev `imova` database. Every run **drops and recreates it** before the first test, then migrates and
seeds it (`tests/Imova.IntegrationTests/TestSupport/TestDatabase.cs`), so each run starts clean —
don't keep anything in it you want to see after the run. Set `IMOVA_TEST_CONNECTION_STRING` to use
another server; the database name must end in `_test` (anything else is refused rather than
dropped). The default is `Host=localhost;Port=5432;Database=imova_test;Username=imova;Password=imova`.

`Imova.IntegrationTests` needs a reachable Postgres (it runs the real `Program.cs` startup,
migrations included) — point it at the running compose Postgres (`localhost:5432`).

If a one-off SDK *container* is ever needed instead (e.g. no local SDK in some other environment),
mount the repo root, not just `src/backend` — `Directory.Build.props`/`Directory.Packages.props`
live at the repo root and MSBuild won't find them otherwise:

```bash
docker run --rm -v "$(pwd)":/repo -v imova-nuget-cache:/root/.nuget/packages -w /repo \
  mcr.microsoft.com/dotnet/sdk:10.0 bash -c "dotnet build src/backend/Imova.sln"
```

The frontend is a Next.js Server Component that fetches from the backend server-side using the
`API_URL` env var, which docker-compose sets to `http://backend:8080` (the Docker service name) —
not `localhost`. If running the frontend outside Docker, `API_URL` defaults to
`http://localhost:8080`.

`SITE_URL` (server-only, default `http://localhost:3000`) is the site's own public address: page
metadata (canonical links, the `og:url` of link previews) and the listing page's JSON-LD use it. Set
it to `https://imova.md` in production, or shared links will point at localhost.

Backend CORS origins come from `Cors:AllowedOrigins` (default `http://localhost:3000`; production
`https://imova.md`) — see "CORS" below.

### Backend only

```bash
cd src/backend
dotnet run --project Imova.Api     # needs a reachable Postgres with PostGIS — e.g. `docker compose up -d postgres`
```

### Frontend only

```bash
cd src/frontend/imova-web
npm install
npm run dev                # requires Node 18+ locally
```

Backend test suite exists (`tests/`, see above) but there's no CI pipeline running it yet, and the
frontend still has no linter config beyond `next lint` and no tests.

### Disk space (WSL2)

Every `docker compose up -d --build` leaves the previous image behind as an untagged
("dangling") image, and BuildKit keeps its build cache. Clean up now and then:

```bash
docker image prune -f && docker builder prune -f   # never `docker volume prune` — that's the DB
```

On WSL2 that alone doesn't give space back to Windows: the distro's virtual disk
(`%LOCALAPPDATA%\Packages\CanonicalGroupLimited.Ubuntu_*\LocalState\ext4.vhdx`) grows but never
shrinks by itself. Make it sparse once, from PowerShell, so freed space is returned:

```powershell
wsl --shutdown
wsl --manage Ubuntu --set-sparse true
```

then, back in WSL, `sudo fstrim -av` hands the currently free blocks back to Windows.

## Secrets and configuration

- Docker Compose reads secrets (`GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `JWT_SIGNING_KEY`,
  `POSTGRES_USER`, `POSTGRES_PASSWORD`) from a root `.env` file — copy `.env.example` to `.env` and
  fill in real values. `.env` is gitignored and must never be committed.
- For running the backend standalone with `dotnet run` (outside Docker), the equivalent secrets
  live in .NET user-secrets rather than `appsettings.Development.json`:

  ```bash
  dotnet user-secrets list --project src/backend/Imova.Api/Imova.Api.csproj
  dotnet user-secrets set "Jwt:Key" "<value>" --project src/backend/Imova.Api/Imova.Api.csproj
  ```

  `appsettings.Development.json` itself is gitignored and kept with empty placeholder values.

## Account emails, rate limiting and proxies

**Sessions.** A login token is only accepted while the account's security stamp is unchanged (the
`sst` claim, checked on every request). Changing or resetting the password, or "Deconectează
celelalte dispozitive" on /account, signs out every other session at once; the session that did it
gets a fresh token. If you rotate or edit `AspNetUsers.SecurityStamp` by hand, that user is signed
out everywhere.

The login token lasts 15 minutes; `src/middleware.ts` in the web app renews it from the
`imova_refresh` cookie before each request (30 days idle with "Ține-mă minte", else 24 hours;
90 days at most — the `Sessions` settings). To watch a renewal locally, set
`Jwt__ExpiryMinutes: "2"` for the backend in docker-compose.yml, sign in, wait a minute and reload:
a new row appears in `RefreshTokens` with the previous one marked `ReplacedAt`.

- **Links in emails** (email confirmation, password reset, new-message notifications) point at
  `App:WebBaseUrl` (compose: `App__WebBaseUrl=http://localhost:3000`; production:
  `https://imova.md`, for the API and the Worker alike). In compose the emails land in
  Mailpit (http://localhost:8025); without an `Email:Host` they're only logged.
- **Link tokens** are ASP.NET data-protection tokens, valid 24 h. The key ring is stored in Postgres
  (`DataProtectionKeys` table), so links survive a backend rebuild/restart. Deleting that table's
  rows invalidates every outstanding link.
- **Auth rate limit** (`RateLimiting:Auth` — `Enabled`, `PermitLimit` = 10, `WindowSeconds` = 60):
  per client IP on login, register, Google sign-in, forgot/reset password, confirm/resend. Hitting it
  locally? Wait a minute, or restart the backend (the counters are in memory).
- **Client IP**: the web app's server actions forward the visitor's IP as `X-Forwarded-For`, and the
  API only believes that header from `ForwardedHeaders:KnownNetworks` (plus loopback). Compose
  trusts `172.16.0.0/12` — fine locally, but in production list only the web server / reverse
  proxy, and put a reverse proxy in front of Next.js (Next keeps a client-sent `X-Forwarded-For`).
- **Login lockout**: 5 wrong passwords lock the account's sign-in for 15 minutes (429). A password
  reset lifts it; so does clearing `LockoutEnd` in `AspNetUsers`.
- **CORS** (`Cors:AllowedOrigins`, array): the origins whose browser JS may call the API directly
  (photo uploads to the SAS URL are separate — that's storage's CORS). Defaults to
  `http://localhost:3000` when unset; production must list the site's own origin(s), e.g.
  `Cors__AllowedOrigins__0=https://imova.md`.
- **Photo uploads** need a signed-in owner: `upload-url`/`confirm` go through the web app's server
  actions (`requestPhotoUploadUrl`/`confirmPhotoUpload`), only the file PUT to storage is done by the
  browser. Before a listing exists, the first user to upload under its id owns that id.

## Worker (background jobs)

`Imova.Worker` runs next to the API (compose service `worker`, same database and email settings)
and hosts scheduled jobs, each on its own timer (`ScheduledJobWorker<TJob>`, jobs implement
`IScheduledJob` and live in Application). It never runs migrations; it starts after the backend and
simply retries a failed run on its next tick.

| Job | Interval setting (default) | What it does |
| --- | --- | --- |
| `SavedSearchAlerts` | `SavedSearchAlerts:IntervalSeconds` (300) | emails new matches for saved searches |
| `FavoriteAlerts` | `FavoriteAlerts:IntervalSeconds` (3600) | emails people who saved a listing when its price changes (≤1 a day per listing) or it ends |
| `ListingExpiry` | `ListingExpiry:IntervalSeconds` (3600) | Active listings live 6 months (`Listing.ActiveMonths`); reminder email 7 days before, then Expired + email |
| `AbandonedPhotoCleanup` | `PhotoCleanup:IntervalSeconds` (21600) | deletes photos (blob + row) whose listing was never created, after 7 days |
| `PhotoSizeBackfill` | `PhotoSizes:IntervalSeconds` (300) | makes the display sizes of photos that lack the current `PhotoSizes.CurrentVersion` (100 per run, newest first) |

Compose sets these to 60 seconds so they're quick to try; emails land in Mailpit. To see
expiry locally, move a listing's `ExpiresAt` in the database (e.g. `now() + interval '3 days'` for
the reminder, `now() - interval '1 minute'` to expire it) and wait a minute.

- It shares the API's data-protection key ring (`ApplicationName` "Imova.Api" + the
  `DataProtectionKeys` table), so the unsubscribe links it puts in emails verify in the API.
- After changing Application/Infrastructure code, rebuild it too:
  `docker compose up -d --build backend worker`.
- Run it outside Docker with `dotnet run --project src/backend/Imova.Worker` and a
  `ConnectionStrings:Default` pointing at the compose Postgres.

## Languages in the address and SEO

The site's language is part of every page address: Romanian has no prefix (`/search`), Russian
and English do (`/ru/search`, `/en/search`). Search engines never send cookies, so this is what lets
them index all three. Every page lives under `src/app/[locale]/`; `src/middleware.ts` maps the
address onto that segment (next-intl, `src/i18n/routing.ts`), after renewing the session.

- **Links and redirects:** use `Link`, `useRouter` and `usePathname` from `@/i18n/navigation`
  (never `next/link` or next/navigation's router), and on the server `return redirectTo(path)` /
  `permanentRedirectTo(path)` from `@/i18n/redirect`. They keep the visitor in their language.
  `notFound` and `useSearchParams` still come from `next/navigation`. A plain `<a>` (downloads,
  route handlers) needs `localizedPath(locale, path)` from `@/i18n/localePaths`.
- **The cookie** (`NEXT_LOCALE`) only steers an unprefixed *page visit*: someone whose language is
  Russian or English who opens `/search` (an old link, a bookmark) is sent to `/ru/search`. Opening
  a prefixed page sets the cookie; the language switcher sets it and moves to the same page in the
  other language. The browser's Accept-Language is ignored on purpose.
- **hreflang:** every page answers with a `Link` header naming its address in all three languages
  (built from `SITE_URL`); listing, agency, search and home pages also set a per-language canonical.
- **Listing addresses:** `/property/100015-apartament-2-camere-botanica` — the short number finds
  it (`GET /api/v1/listings/by-number/{n}`), the words come from the title
  (`lib/listing/listingUrl.ts`, Romanian diacritics and Cyrillic in Latin letters). Any other
  words, or an old `/property/<guid>` link, get a permanent redirect to that address.
- **Sitemap and robots:** `/sitemap.xml` is an index of `/sitemaps/pages.xml` (home, agencies,
  search per transaction × type, agency pages) and `/sitemaps/listings-N.xml` (10 000 public
  listings each, from `GET /api/v1/sitemap/listings?page=N`), each page once per language with
  its hreflang alternates. `/robots.txt` keeps private pages out in every language. Both are
  built per request — `SITE_URL` must be the real domain in production.
- **Search pages:** only plain searches (transaction, one type, a place, page 1) are indexed;
  anything narrower is `noindex, follow` (`lib/search/indexing.ts`).
- **Emails and Telegram** link to the recipient's language (`AppOptions.WebUrl(path, language)`).
- **Share card:** pages without their own image use `public/images/og-default.jpg` (1200×630).

### After launch: search engines

Tracked as Jira **KAN-33**. On launch day, once `https://imova.md` is live (`SITE_URL` and
`App:WebBaseUrl` = `https://imova.md`):

1. Check `https://imova.md/robots.txt` ends with `Sitemap: https://imova.md/sitemap.xml` (localhost
   there means `SITE_URL` isn't set) and that `/sitemap.xml` opens.
2. **Google Search Console** — add a *Domain* property `imova.md`, verify with the DNS TXT record at
   the registrar, then *Sitemaps* → submit `sitemap.xml`.
3. **Bing Webmaster Tools** — *Import from Google Search Console*.
4. **Yandex Webmaster** — add `https://imova.md`, verify, add the sitemap.
5. A week later: Search Console → *Pages* shows listings being indexed (about 3 × the active
   listings — one per language).

## Telegram bot

Alerts can also go to Telegram. The bot runs in the **Worker** (`TelegramUpdatePoller`,
long polling — no webhook, no public endpoint); the API only builds the "Conectează Telegram"
link and never sees the token.

- **One bot per environment.** Telegram lets only one process poll a bot (a second gets 409), so
  local work uses its own dev bot, never the production one. Create bots with @BotFather (`/newbot`).
- **Locally:** put the dev bot in `.env` — `TELEGRAM_BOT_TOKEN` (`1234567890:` + 35 characters,
  plain ASCII, no quotes or spaces) and `TELEGRAM_BOT_USERNAME` (without the @). Compose passes the
  username to the backend and both to the worker. Without a username the account page shows no
  Telegram block; without a token the worker doesn't start the bot. Outside Docker:
  `dotnet user-secrets set "Telegram:BotToken" "<token>" --project src/backend/Imova.Worker`.
- **Production:** the live bot's token goes in Key Vault, only for the Worker.
- **Connecting:** /account → Notificări → "Conectează Telegram" gives a `t.me/<bot>?start=<code>`
  link (one-time code, 15 minutes, stored as a SHA-256 hash). Pressing Start connects that
  Telegram account (one chat per account, one account per chat); `/stop` or "Deconectează"
  disconnects; a chat that blocks the bot is dropped. Commands: `/cautari` (saved searches), `/stop`.
- On start the worker sets the bot's command menu and descriptions (ro default, en, ru).
- **Saved-search alerts:** each search has `AlertChannels` (Email, Telegram or both; set on
  /saved-searches). A new search gets Telegram too when the account has it connected. The alert job
  (`SavedSearchAlerts`, every 60 s) sends one Telegram message per listing (photo, price, place,
  "Vezi anunțul" + "Oprește alerta", max 5, then "and N more"); a Telegram-only search whose chat is
  gone falls back to email. Photos on a URL Telegram can't reach (local Azurite) are uploaded instead.
- To see an alert locally without publishing anything, move a saved search's watermark back:
  `update "SavedSearches" set "AlertFrequency"=2, "LastAlertedUpTo"=now()-interval '90 days' where "Id"='…';`
  — within a minute its existing matches arrive as new.
- Telegram won't link to `localhost` (a button to it rejects the message, a text link is dropped), so
  locally the "Vezi anunțul" / saved-search links are simply left out; on the real domain they're there.
- The HttpClient for the Bot API logs nothing (the token is in every request path) — keep it that way.
- A 404 from every Bot API call means the token is wrong.

## Photo sizes

Every listing photo is shown from JPEG copies made from the upload, never from the original:
`{guid}_400.jpg` (thumbnails), `_800.jpg` (cards, alert emails) and `_1600.jpg` (listing page,
viewer, `og:image`), next to the original in the same container (`PhotoSizes` in
Application/Features/Media/Sizes). They're made with ImageMagick (Magick.NET, `MagickPhotoResizer`)
when an upload is confirmed — upright (EXIF orientation), sRGB, metadata (GPS!) stripped, never
enlarged; HEIC/HEIF uploads become visible in every browser this way. A file ImageMagick can't
read is refused (`upload.notAnImage`); a storage hiccup only leaves the sizes to the Worker's
`PhotoSizeBackfill`, and until then the DTO's three URLs are the original. To remake every photo's
sizes (new sizes, other quality), bump `PhotoSizes.CurrentVersion`. Server-written blobs are
served with `Cache-Control: public, max-age=31536000, immutable` (their names never get new
content). The Docker images publish for `linux-x64` only, which keeps the other platforms' native
ImageMagick builds (~240 MB) out of them.
