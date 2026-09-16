---
name: run-stack
description: How to run, build, and test the IMOVA stack locally — docker compose commands, the backend-only/frontend-only dev loops, EF Core migrations, and a dotnet test/vstest quirk specific to this sandbox. Use when starting, running, building, or testing this app, or troubleshooting the dev environment.
---

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
