# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

IMOVA — a real estate listings marketplace for Moldova (individuals + agencies), aimed at
competing with 999.md. This is currently an early, intentionally minimal skeleton (v0.1) built up
incrementally rather than the full target architecture — do not assume unbuilt pieces (auth,
database, search, admin) exist yet.

The full target stack (to be introduced incrementally, not all at once) is: ASP.NET Core / .NET,
Clean Architecture + Vertical Slices, EF Core, PostgreSQL + PostGIS, Redis, Next.js/React/TypeScript,
Tailwind + shadcn/ui, FluentValidation, MediatR. When adding features, prefer the simplest thing
that works over front-loading layers (Domain/Application/Infrastructure separation, repository
pattern, etc.) that aren't yet justified by real complexity — this was an explicit decision, not an
oversight.

## Repository layout

```
.
├── docker-compose.yml
└── src/
    ├── backend/Imova.Api/        ASP.NET Core (.NET 10) minimal API
    └── frontend/imova-web/       Next.js 15 (App Router, TypeScript)
```

There is no solution (`.sln`) file and no Domain/Application/Infrastructure/Contracts projects yet
— just a single `Imova.Api` project using top-level minimal APIs in `Program.cs` (no controllers).
The frontend is a single Next.js app with no component/feature folder structure yet beyond the
default `app/` directory.

## Running the stack

Run both services together via Docker Compose (this is the primary way to run and test the app —
there is no local .NET SDK or a modern-enough Node version in this dev environment, only Docker):

```bash
docker compose up -d --build   # build and start both containers
docker compose ps              # check status
docker compose logs backend    # or frontend
docker compose down            # stop
```

- Backend: `http://localhost:8080` (e.g. `GET /health`, `GET /api/v1/properties`)
- Frontend: `http://localhost:3000`

The frontend is a Next.js Server Component that fetches from the backend server-side using the
`API_URL` env var, which docker-compose sets to `http://backend:8080` (the Docker service name) —
not `localhost`. If running the frontend outside Docker, `API_URL` defaults to
`http://localhost:8080`.

Backend CORS is currently locked to `http://localhost:3000` (`Program.cs`); update the `"Frontend"`
CORS policy if the frontend origin changes.

### Backend only

```bash
cd src/backend/Imova.Api
dotnet run                 # requires local .NET 10 SDK
```

### Frontend only

```bash
cd src/frontend/imova-web
npm install
npm run dev                # requires Node 18+ locally
```

No test suite, linter config beyond `next lint`, or CI pipeline exists yet.
