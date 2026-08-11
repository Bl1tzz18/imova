# IMOVA

Platformă de anunțuri imobiliare pentru Republica Moldova, orientată spre persoane fizice
(spre deosebire de 999.md, dominat de agenți). v0.1 — schelet minimal, construit incremental.

## Stack

- **Backend**: ASP.NET Core / .NET 10
- **Frontend**: Next.js 15 + React + TypeScript
- Țintă pe termen mediu (nu totul e implementat încă): Clean Architecture + Vertical Slices,
  EF Core, PostgreSQL + PostGIS, Redis, Tailwind + shadcn/ui, FluentValidation, MediatR.

## Structură

```
imova/
├── docker-compose.yml
└── src/
    ├── backend/Imova.Api/        ASP.NET Core (.NET 10) minimal API
    └── frontend/imova-web/       Next.js 15 (App Router, TypeScript)
```

## Rulare

Cel mai simplu mod de a rula totul este via Docker Compose:

```bash
docker compose up -d --build
```

- Backend: http://localhost:8080 (`GET /health`, `GET /api/v1/properties`)
- Frontend: http://localhost:3000

Oprire:

```bash
docker compose down
```

### Backend separat (necesită .NET 10 SDK local)

```bash
cd src/backend/Imova.Api
dotnet run
```

### Frontend separat (necesită Node 18+ local)

```bash
cd src/frontend/imova-web
npm install
npm run dev
```

Vezi `CLAUDE.md` pentru detalii de arhitectură și convenții de dezvoltare.
