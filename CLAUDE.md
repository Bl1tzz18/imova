# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

IMOVA — a real estate listings marketplace for Moldova (individuals + agencies), aimed at
competing with 999.md. This is still an early build (v0.1) grown incrementally rather than the
full target architecture — do not assume unbuilt pieces (auth, search, admin, PostGIS, Redis)
exist yet.

The full target stack (to be introduced incrementally, not all at once) is: ASP.NET Core / .NET,
Clean Architecture + Vertical Slices, EF Core, PostgreSQL + PostGIS, Redis, Next.js/React/TypeScript,
Tailwind + shadcn/ui, FluentValidation, MediatR. The backend now uses Clean Architecture
(Domain/Infrastructure/Api) + Vertical Slices, EF Core + PostgreSQL, MediatR and FluentValidation
— see below. For anything beyond that, keep preferring the simplest thing that works over
front-loading structure that isn't yet justified by real complexity.

## Repository layout

```
.
├── docker-compose.yml
└── src/
    ├── backend/
    │   ├── Imova.sln
    │   ├── Imova.Domain/          Entities only, no dependencies (e.g. Properties/Property.cs)
    │   ├── Imova.Infrastructure/  EF Core + PostgreSQL: ImovaDbContext, entity configs, migrations
    │   └── Imova.Api/             ASP.NET Core (.NET 10) minimal API — vertical slices + DI wiring
    └── frontend/imova-web/        Next.js 15 (App Router, TypeScript)
```

`Imova.Api` contains the vertical slices themselves (not a separate `Application` project):
`Features/<Feature>/<UseCase>/` holds a MediatR command or query, its handler, and (for commands)
a FluentValidation validator, plus an endpoint-mapping extension method. See
`Features/Properties/` (`GetProperties`, `CreateProperty`) as the reference implementation —
follow this same shape for new features. `Common/Behaviors/ValidationBehavior.cs` is a MediatR
pipeline behavior that runs every slice's validator automatically; validation failures are turned
into a `400` with a `ValidationProblem` body by the global exception handler in `Program.cs` — new
slices don't need to repeat that wiring, just add a validator class next to the command.

The frontend is a single Next.js app with no component/feature folder structure yet beyond the
default `app/` directory.

## Running the stack

Run all services together via Docker Compose (this is the primary way to run and test the app —
there is no local .NET SDK or a modern-enough Node version in this dev environment, only Docker):

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
`Program.cs`) — no manual migration step needed to run the stack. To add a new migration after
changing an entity or its configuration, use a one-off SDK container (no local .NET SDK here),
e.g. from `src/backend/Imova.Api`:

```bash
docker run --rm -v "$(pwd)/..":/src -w /src/Imova.Api mcr.microsoft.com/dotnet/sdk:10.0 bash -c \
  "dotnet tool install --tool-path /tmp/tools dotnet-ef && \
   /tmp/tools/dotnet-ef migrations add <Name> --project ../Imova.Infrastructure --startup-project ."
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
dotnet run --project Imova.Api     # requires local .NET 10 SDK + a reachable Postgres
```

### Frontend only

```bash
cd src/frontend/imova-web
npm install
npm run dev                # requires Node 18+ locally
```

No test suite, linter config beyond `next lint`, or CI pipeline exists yet.
