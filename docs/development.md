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

Backend CORS is currently locked to `http://localhost:3000` (`Program.cs`); update the `"Frontend"`
CORS policy if the frontend origin changes.

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

- **Links in emails** (email confirmation, password reset, new-message notifications) point at
  `App:WebBaseUrl` (compose: `App__WebBaseUrl=http://localhost:3000`). In compose the emails land in
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
| `ListingExpiry` | `ListingExpiry:IntervalSeconds` (3600) | Active listings live 6 months (`Listing.ActiveMonths`); reminder email 7 days before, then Expired + email |
| `AbandonedPhotoCleanup` | `PhotoCleanup:IntervalSeconds` (21600) | deletes photos (blob + row) whose listing was never created, after 7 days |

Compose sets all three to 60 seconds so they're quick to try; emails land in Mailpit. To see
expiry locally, move a listing's `ExpiresAt` in the database (e.g. `now() + interval '3 days'` for
the reminder, `now() - interval '1 minute'` to expire it) and wait a minute.

- It shares the API's data-protection key ring (`ApplicationName` "Imova.Api" + the
  `DataProtectionKeys` table), so the unsubscribe links it puts in emails verify in the API.
- After changing Application/Infrastructure code, rebuild it too:
  `docker compose up -d --build backend worker`.
- Run it outside Docker with `dotnet run --project src/backend/Imova.Worker` and a
  `ConnectionStrings:Default` pointing at the compose Postgres.
