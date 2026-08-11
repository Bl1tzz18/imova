# IMOVA

Real estate listings platform for the Republic of Moldova, aimed at individuals
(as opposed to 999.md, dominated by agencies). v0.1 — minimal skeleton, built incrementally.

## Stack

- **Backend**: ASP.NET Core / .NET 10 — Clean Architecture (Domain/Infrastructure/Api) +
  Vertical Slices, EF Core + PostgreSQL, MediatR, FluentValidation
- **Frontend**: Next.js 15 + React + TypeScript
- Medium-term target (not all implemented yet): PostGIS, Redis, Tailwind + shadcn/ui.

## Structure

```
imova/
├── docker-compose.yml
└── src/
    ├── backend/
    │   ├── Imova.Domain/          entities
    │   ├── Imova.Infrastructure/  EF Core + PostgreSQL
    │   └── Imova.Api/             API + vertical slices (Features/...)
    └── frontend/imova-web/        Next.js 15 (App Router, TypeScript)
```

## Running

The simplest way to run everything is via Docker Compose:

```bash
docker compose up -d --build
```

- Backend: http://localhost:8080 (`GET /health`, `GET /api/v1/properties`)
- Frontend: http://localhost:3000
- Postgres: localhost:5432 (`imova`/`imova`/`imova`)

Stop:

```bash
docker compose down
```

### Backend only (requires local .NET 10 SDK + reachable Postgres)

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
