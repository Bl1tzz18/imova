# IMOVA

Platformă de anunțuri imobiliare pentru Republica Moldova, orientată spre persoane fizice
(spre deosebire de 999.md, dominat de agenți). v0.1 — schelet minimal, construit incremental.

## Stack

- **Backend**: ASP.NET Core / .NET 10 — Clean Architecture (Domain/Infrastructure/Api) +
  Vertical Slices, EF Core + PostgreSQL, MediatR, FluentValidation
- **Frontend**: Next.js 15 + React + TypeScript
- Țintă pe termen mediu (nu totul e implementat încă): PostGIS, Redis, Tailwind + shadcn/ui.

## Structură

```
imova/
├── docker-compose.yml
└── src/
    ├── backend/
    │   ├── Imova.Domain/          entități
    │   ├── Imova.Infrastructure/  EF Core + PostgreSQL
    │   └── Imova.Api/             API + vertical slices (Features/...)
    └── frontend/imova-web/        Next.js 15 (App Router, TypeScript)
```

## Rulare

Cel mai simplu mod de a rula totul este via Docker Compose:

```bash
docker compose up -d --build
```

- Backend: http://localhost:8080 (`GET /health`, `GET /api/v1/properties`)
- Frontend: http://localhost:3000
- Postgres: localhost:5432 (`imova`/`imova`/`imova`)

Oprire:

```bash
docker compose down
```

### Backend separat (necesită .NET 10 SDK local + Postgres accesibil)

```bash
cd src/backend
dotnet run --project Imova.Api
```

### Frontend separat (necesită Node 18+ local)

```bash
cd src/frontend/imova-web
npm install
npm run dev
```

Vezi `CLAUDE.md` pentru detalii de arhitectură și convenții de dezvoltare.
