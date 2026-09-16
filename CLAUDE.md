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

## Code organization

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

## Running the stack

See the `run-stack` skill (`.claude/skills/run-stack/SKILL.md`) for docker compose commands,
backend/frontend-only dev loops, EF Core migrations, and dev-environment gotchas.
