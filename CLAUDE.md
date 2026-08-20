# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

IMOVA — a real estate listings marketplace for Moldova (individuals + agencies), aimed at
competing with 999.md. This is still an early build (v0.1) grown incrementally rather than the
full target architecture — do not assume unbuilt pieces (auth, search, admin, Redis) exist yet.

There is no auth yet — no login, no registration endpoint, no JWT. `Imova.Domain.Users.User`
exists as a domain concept and DB table (seeded with one demo user), but the only way a `User` row
gets created right now is that seed. `CreatePropertyCommand.OwnerId` is accepted directly from the
request body as a stopgap (see the comment on that record) — once auth exists, pull it from the
caller's claims instead and stop trusting the client for it.

The full target stack (to be introduced incrementally, not all at once) is: ASP.NET Core / .NET,
Clean Architecture + Vertical Slices, EF Core, PostgreSQL + PostGIS, Redis, Next.js/React/TypeScript,
Tailwind + shadcn/ui, FluentValidation, MediatR. The backend now uses Clean Architecture
(Domain/Contracts/Application/Infrastructure/Api/Worker) + Vertical Slices, EF Core + PostgreSQL +
PostGIS, MediatR and FluentValidation — see below. For anything beyond that, keep preferring the
simplest thing that works over front-loading structure that isn't yet justified by real complexity.

`Imova.Domain` has DDD building blocks in `Common/` (`Entity`, `AggregateRoot`, `IDomainEvent` — no
domain events raised yet, just the base types) plus three aggregates/entities:
`Properties/Property.cs` (lifecycle: `Draft` → `Published`/`Rejected`/... via `Publish()`/`Archive()`,
see `PropertyStatus`), `Users/User.cs`, and `Locations/PropertyLocation.cs` (a separate entity keyed
by `PropertyId`, not a navigation property on `Property` — holds a PostGIS `Point` via
NetTopologySuite). Postgres runs as `postgis/postgis:16-3.4` in compose (plain `postgres:16` doesn't
have the extension available, and `CREATE EXTENSION postgis` fails against it) and
`OnModelCreating` calls `HasPostgresExtension("postgis")`.

## Repository layout

```
.
├── docker-compose.yml
├── Directory.Build.props      Shared MSBuild properties (TargetFramework, Nullable, ...)
├── Directory.Packages.props   Central NuGet package version management for all .csproj files
├── .editorconfig
├── src/
│   ├── backend/
│   │   ├── Imova.sln
│   │   ├── Imova.Domain/          Entities only, no dependencies (e.g. Properties/Property.cs)
│   │   ├── Imova.Contracts/       Wire-format DTOs shared by Application + Api, no dependencies
│   │   ├── Imova.Application/     MediatR commands/queries/handlers/validators — the vertical slices
│   │   ├── Imova.Infrastructure/  EF Core + PostgreSQL: ImovaDbContext, entity configs, migrations
│   │   ├── Imova.Api/             ASP.NET Core (.NET 10) minimal API — endpoint mapping + DI wiring
│   │   └── Imova.Worker/          Background-job host (scaffolded, no jobs yet, not in compose)
│   └── frontend/imova-web/        Next.js 15 (App Router, TypeScript)
└── tests/
    ├── Imova.UnitTests/           Domain + Application logic, no external dependencies
    ├── Imova.IntegrationTests/    WebApplicationFactory<Program> against Imova.Api
    └── Imova.ArchitectureTests/   NetArchTest rules enforcing the layering below
```

Slices live in `Imova.Application/Features/<Feature>/<UseCase>/`: a MediatR command or query, its
handler, and (for commands) a FluentValidation validator. See `Features/Properties/`
(`GetProperties`, `CreateProperty`) as the reference implementation — follow this same shape for
new features. `Common/Behaviors/ValidationBehavior.cs` is a MediatR pipeline behavior that runs
every slice's validator automatically; validation failures are turned into a `400` with a
`ValidationProblem` body by the global exception handler in `Imova.Api/Program.cs` — new slices
don't need to repeat that wiring, just add a validator class next to the command.

`Imova.Api` stays thin: `Program.cs` (composition root — DI wiring, migration-on-startup, exception
handling) plus one `Features/<Feature>/<UseCase>/<UseCase>Endpoint.cs` per slice that maps the route
and sends the command/query via MediatR. `Imova.Contracts` holds the DTOs returned over the wire
(e.g. `PropertyDto`) and must not reference `Imova.Domain` — `Imova.Application` does the
entity-to-DTO mapping (see `PropertyMapping.cs`). Layering is enforced by
`tests/Imova.ArchitectureTests`, not just convention — run it after moving code between projects.

The frontend is a single Next.js app with no component/feature folder structure yet beyond the
default `app/` directory.

## Running the stack

Run all services together via Docker Compose — this is the primary way to run and test the app as
a whole (it's the only way to get PostGIS, and matches how it actually deploys):

```bash
docker compose up -d --build   # build and start postgres, backend, frontend
docker compose ps              # check status
docker compose logs backend    # or frontend / postgres
docker compose down            # stop (add -v to also drop the postgres volume)
```

- Backend: `http://localhost:8080` (e.g. `GET /health`, `GET /api/v1/properties`)
- Frontend: `http://localhost:3000`
- Postgres: `localhost:5432` (`imova`/`imova`/`imova` — user/password/db)

The backend applies EF Core migrations automatically on startup (`dbContext.Database.Migrate()` in
`Program.cs`) — no manual migration step needed to run the stack.

A local .NET 10 SDK is installed (`~/.dotnet`, on `PATH` via `~/.bashrc`/`~/.profile`) so `dotnet
build`/`dotnet run`/`dotnet ef` work directly without a container — much faster for quick
build/test loops than round-tripping through Docker. Use it for that; still use Docker Compose to
actually run/verify the app, since PostGIS only exists there. To add a migration after changing an
entity or its configuration, from the repo root:

```bash
dotnet tool install --tool-path /tmp/ef-tools dotnet-ef   # once per shell/session
/tmp/ef-tools/dotnet-ef migrations add <Name> \
  --project src/backend/Imova.Infrastructure --startup-project src/backend/Imova.Api
```

Build/test the whole solution the normal way — `dotnet build src/backend/Imova.sln`. For running
tests, `dotnet test`'s own console output was observed to go silent in this sandbox (exit code 0,
no output) while `dotnet vstest <path-to-dll>` printed normally; try `dotnet test` first and fall
back to `dotnet vstest tests/<Project>/bin/Debug/net10.0/<Project>.dll` if it goes quiet.
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
