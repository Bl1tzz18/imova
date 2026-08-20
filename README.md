# IMOVA

Real estate listings platform for the Republic of Moldova, aimed at individuals
(as opposed to 999.md, dominated by agencies). v0.1 — minimal skeleton, built incrementally.

## Stack

- **Backend**: ASP.NET Core / .NET 10 — Clean Architecture
  (Domain/Contracts/Application/Infrastructure/Api/Worker) + Vertical Slices, EF Core + PostgreSQL +
  PostGIS, MediatR, FluentValidation
- **Frontend**: Next.js 15 + React + TypeScript
- Medium-term target (not all implemented yet): auth, search, admin, Redis, Tailwind + shadcn/ui.

## Structure

```
imova/
├── docker-compose.yml
├── Directory.Build.props      shared MSBuild properties
├── Directory.Packages.props   central NuGet package versions
├── src/
│   ├── backend/
│   │   ├── Imova.Domain/          entities
│   │   ├── Imova.Contracts/       wire-format DTOs
│   │   ├── Imova.Application/     vertical slices (Features/...): commands, queries, validators
│   │   ├── Imova.Infrastructure/  EF Core + PostgreSQL
│   │   ├── Imova.Api/             endpoint mapping + DI wiring
│   │   └── Imova.Worker/          background-job host (scaffolded, not wired up yet)
│   └── frontend/imova-web/        Next.js 15 (App Router, TypeScript)
└── tests/
    ├── Imova.UnitTests/
    ├── Imova.IntegrationTests/
    └── Imova.ArchitectureTests/
```

## Running

The simplest way to run everything is via Docker Compose:

```bash
docker compose up -d --build
```

- Backend: http://localhost:8080 (`GET /health`, `GET /api/v1/properties`)
- Frontend: http://localhost:3000
- Postgres (PostGIS-enabled, `postgis/postgis:16-3.4`): localhost:5432 (`imova`/`imova`/`imova`)

Stop:

```bash
docker compose down
```

### Backend only (requires local .NET 10 SDK + a reachable Postgres with PostGIS)

```bash
cd src/backend
dotnet run --project Imova.Api
```

### Frontend only (requires Node 18+ locally)

```bash
cd src/frontend/imova-web
npm install
npm run dev
```

See `CLAUDE.md` for architecture details and development conventions.
